use serde::Deserialize;

// #[serde(rename_all = "camelCase")] because .NET backend
// serializes with camelCase (lobbyId, playerCount, etc.)
// but Rust convention is snake_case. Serde handles translation.

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct LobbyCreated {
    pub lobby_id: String,
    pub name: String,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PlayerJoined {
    pub lobby_id: String,
    pub player: PlayerInfo,
    pub player_count: i32,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PlayerReady {
    pub lobby_id: String,
    pub player_id: String,
    pub is_ready: bool,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GameStarted {
    pub lobby_id: String,
    pub players: Vec<GamePlayerInfo>,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GamePlaying {
    pub lobby_id: String,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GameOver {
    pub lobby_id: String,
    pub winner_id: String,
    pub reason: String,
    pub results: Vec<GameResult>,
    pub timestamp: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GameResult {
    pub id: String,
    pub name: String,
    pub platform: String,
    pub value: f32,
    pub tap_count: i32,
    pub rank: i32,
}

// Shared sub-types

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PlayerInfo {
    pub id: String,
    pub name: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct GamePlayerInfo {
    pub id: String,
    pub name: String,
    pub platform: String,
}
