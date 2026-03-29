use lapin::{Connection, ConnectionProperties, options::*, types::FieldTable};
#[tokio::main]
async fn main() {
    println!("Connecting to RabbitMQ...");

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
}
