mod events;
mod logger;
use lapin::{
    Connection, ConnectionProperties, options::*, protocol::exchange::Delete, types::FieldTable,
};
use tokio_stream::StreamExt;
#[tokio::main]
async fn main() {
    println!("{}", "=".repeat(50));
    println!("ToTheTop - Rust Event Logger");
    println!("Connecting to RabbitMQ...");
    println!("{}", "=".repeat(50));

    let connection = Connection::connect(
        "amqp://guest:guest@localhost:5672",
        ConnectionProperties::default(),
    )
    .await
    .expect("Failed to connect to RabbitMQ");

    println!("Connected to RabbitMQ!");

    let channel = connection
        .create_channel()
        .await
        .expect("Failed to create channel");
    println!("Channel created (id: {})", channel.id());

    let queue = channel
        .queue_declare(
            "rust-logger",
            QueueDeclareOptions {
                exclusive: false,
                durable: false,
                auto_delete: true,
                ..Default::default()
            },
            FieldTable::default(),
        )
        .await
        .expect("Failed to declare queue");

    println!("Declared queue: {}", queue.name());

    channel
        .queue_bind(
            queue.name().as_str(),
            "lobby_events", // exchange the .NET backend publishes to
            "#",            // matching all routing keys
            QueueBindOptions::default(),
            FieldTable::default(),
        )
        .await
        .expect("Failed to bind queue");

    println!("Bound queue to exchange 'lobby_events' with routing key '#'");
    println!("Waiting for events...\n");
    // creating consumer on queue
    let mut consumer = channel
        .basic_consume(
            queue.name().as_str(),
            "rust-logger-consumer",
            BasicConsumeOptions::default(),
            FieldTable::default(), 
        ).await.expect("Failed to create consumer");
    
    // event loop
    while let Some(delivery_result) = consumer.next().await {
        match delivery_result {
            Ok(delivery) => {
                let routing_key = delivery.routing_key.as_str();
                
                let payload = std::str::from_utf8(&delivery.data)
                .unwrap_or("{}");
                
                match routing_key{
                    "lobby.created" => {
                        if let Ok(event) = serde_json::from_str::<events::LobbyCreated>(payload) {
                            logger::log_lobby_created(&event);
                        }
                    }
                    "lobby.player.joined" => {
                        if let Ok(event) = serde_json::from_str::<events::PlayerJoined>(payload) {
                            logger::log_player_joined(&event);
                        }
                    }
                    "lobby.player.ready" => {
                        if let Ok(event) = serde_json::from_str::<events::PlayerReady>(payload) {
                            logger::log_player_ready(&event);
                        }
                    }
                    "game.started" => {
                        if let Ok(event) = serde_json::from_str::<events::GameStarted>(payload) {
                            logger::log_game_started(&event);
                        }
                    }
                    "game.playing" => {
                        if let Ok(event) = serde_json::from_str::<events::GamePlaying>(payload) {
                            logger::log_game_playing(&event);
                        }
                    }
                    "game.over" => {
                        if let Ok(event) = serde_json::from_str::<events::GameOver>(payload) {
                            logger::log_game_over(&event);
                        }
                    }
                    // Skip noisy events
                    "game.tick" => {}
                    // Log unknown events so we notice if we're missing something
                    _ => logger::log_unknown(routing_key),                    
                }
                
                
                // message acknowledgment
                delivery
                .ack(BasicAckOptions::default())
                .await
                .expect("Failed to ack");
            }
            Err(e)=>{
                eprintln!("Error receiving message: {}", e);
            }
        }
    }

}
