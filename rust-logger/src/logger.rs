use core::time;

use chrono::Local;
use colored::*;

use crate::events::*;

fn timestamp() -> String {
    Local::now()
        .format("%H:%M:%S")
        .to_string()
        .dimmed()
        .to_string()
}

fn short_id(id: &str) -> String {
    id[..8.min(id.len())].to_string()
}

pub fn log_lobby_created(event: &LobbyCreated) {
    println!(
        "{} {} Lobby \"{}\" created ({})",
        timestamp(),
        "LOBBY".cyan().bold(),
        event.name.white().bold(),
        short_id(&event.lobby_id).dimmed()
    );
}

pub fn log_player_joined(event: &PlayerJoined) {
    println!(
        "{} {}  {} joined lobby {} ({} players)",
        timestamp(),
        "JOIN".green().bold(),
        event.player.name.white().bold(),
        short_id(&event.lobby_id).dimmed(),
        event.player_count.to_string().yellow()
    );
}

pub fn log_player_ready(event: &PlayerReady) {
    let status = if event.is_ready {
        "READY".green().bold()
    } else {
        "UNREADY".red().bold()
    };

    println!(
        "{} {} Player {} is {} in lobby {}",
        timestamp(),
        "STATE".magenta().bold(),
        short_id(&event.player_id).white(),
        status,
        short_id(&event.lobby_id).dimmed()
    );
}

pub fn log_game_started(event: &GameStarted) {
    println!(
        "\n{} {} Game starting in lobby {} with {} players:",
        timestamp(),
        "GAME".yellow().bold(),
        short_id(&event.lobby_id).dimmed(),
        event.players.len().to_string().yellow()
    );

    for player in &event.players {
        let platform_icon = match player.platform.as_str() {
            "browser" => "[WEB]".cyan(),
            _ => "[PC]".blue(),
        };
        println!(
            "         {} {} {}",
            platform_icon,
            player.name.white(),
            short_id(&player.id).dimmed()
        );
    }
    println!();
}

pub fn log_game_playing(event: &GamePlaying) {
    println!(
        "{} {} Match is LIVE in lobby {}",
        timestamp(),
        "GAME".yellow().bold(),
        short_id(&event.lobby_id).dimmed()
    );
}

pub fn log_game_over(event: &GameOver) {
    let reason_text = match event.reason.as_str() {
        "reached_top" => "reached the top!",
        "timeout" => "time ran out",
        _ => &event.reason,
    };

    println!(
        "\n{} {} Game over in lobby {} — {}",
        timestamp(),
        "FINISH".red().bold(),
        short_id(&event.lobby_id).dimmed(),
        reason_text.white()
    );

    // Build all lines first so we can measure the widest one
    let mut lines: Vec<(String, bool)> = Vec::new();

    for result in &event.results {
        let is_winner = result.id == event.winner_id;
        let crown = if is_winner { "👑" } else { "" };
        let platform = match result.platform.as_str() {
            "browser" => "[WEB]",
            _ => "[PC] ",
        };

        let line = format!(
            "#{} {} {:<15} {:>3} taps {:>5.1}%  {}",
            result.rank, platform, result.name,
            result.tap_count, result.value, crown
        );

        lines.push((line, is_winner));
    }

    // Find the longest line (adding 1 for emoji line)
    let max_len = lines.iter().map(|(l, winner)| {
        l.chars().count() + if *winner { 1 } else { 0 }
    }).max().unwrap_or(0);

    let border = "─".repeat(max_len + 2);
    println!("         ┌{}┐", border.dimmed());

    for (line, is_winner) in &lines {
        let extra = if *is_winner { 1 } else { 0 };
        let pad = max_len - line.chars().count() - extra;
        let padded = format!("{}{}", line, " ".repeat(pad));

        if *is_winner {
            println!("         │ {} │", padded.yellow().bold());
        } else {
            println!("         │ {} │", padded);
        }
    }

    println!("         └{}┘\n", border.dimmed());
}

pub fn log_unknown(routing_key: &str) {
    println!(
        "{} {} {}",
        timestamp(),
        "???".dimmed(),
        routing_key.dimmed()
    );
}
