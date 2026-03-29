export interface Player {
  id: string
  name: string
  isReady: boolean
  joinedAt: string
  platform?: string
}

export interface Lobby {
  id: string
  name: string
  players: Player[]
  maxPlayers: number
  state: string
}

export interface GamePlayer {
  id: string
  name: string
  platform: string
  value: number
  tapCount: number
}

export interface GameState {
  players: GamePlayer[]
  timeRemaining: number
}

export interface GameResult extends GamePlayer {
  rank: number
}

export interface GameOverData {
  winnerId: string
  reason: string
  results: GameResult[]
}

export type GamePhase = 'lobby' | 'countdown' | 'playing' | 'results'
