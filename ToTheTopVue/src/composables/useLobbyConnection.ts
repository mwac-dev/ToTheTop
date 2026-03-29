import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr"
import { onMounted, onUnmounted, ref, shallowRef, triggerRef } from "vue"
import type { Lobby, Player, GamePhase, GameState, GameOverData } from "@/types"

const API_BASE = 'http://localhost:5082'

export function useLobbyConnection(lobbyId: string) {
    const lobby = ref<Lobby | null>(null)
    const connectionStatus = ref<string>('disconnected')
    const events = ref<Array<{ type: string; data: any; time: Date }>>([])

    // Game state
    const gamePhase = ref<GamePhase>('lobby')
    const gameState = shallowRef<GameState | null>(null)
    const countdown = ref<number>(0)
    const gameResults = ref<GameOverData | null>(null)
    const playerId = ref<string | null>(null)
    const isReady = ref<boolean>(false)

    let connection: HubConnection | null = null

    function addEvent(type: string, data: any) {
        events.value.unshift({ type, data, time: new Date() })
        if (events.value.length > 50) events.value.pop()
    }

    async function connect() {
        connection = new HubConnectionBuilder()
            .withUrl(`${API_BASE}/hub/lobby`)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build()

        // Lobby handlers
        connection.on('PlayerJoined', (data: { player: Player; lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('PlayerJoined', data.player)
        })

        connection.on('PlayerReady', (data: { playerId: string; isReady: boolean; lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('PlayerReady', { playerId: data.playerId, isReady: data.isReady })
        })

        connection.on('PlayerLeft', (data: { playerId: string; lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('PlayerLeft', { playerId: data.playerId })
        })

        connection.on('GameStarting', (data: { lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('GameStarting', { players: data.lobby.players })
        })

        // Game handlers
        connection.on('GameCountdown', (data: { countdown: number }) => {
            gamePhase.value = 'countdown'
            countdown.value = data.countdown
        })

        connection.on('GamePlaying', (data: { duration: number }) => {
            gamePhase.value = 'playing'
            addEvent('GamePlaying', data)
        })

        connection.on('GameTick', (data: { players: GameState['players']; timeRemaining: number }) => {
            gameState.value = { players: data.players, timeRemaining: data.timeRemaining }
            triggerRef(gameState)
        })

        connection.on('GameOver', (data: GameOverData) => {
            gamePhase.value = 'results'
            gameResults.value = data
            addEvent('GameOver', { winnerId: data.winnerId, reason: data.reason })
        })

        // Connection lifecycle
        connection.onreconnecting(() => { connectionStatus.value = 'reconnecting' })
        connection.onreconnected(async () => {
            connectionStatus.value = 'connected'
            if (connection) {
                await connection.invoke('JoinLobbyGroup', lobbyId, playerId.value)
            }
        })
        connection.onclose(() => { connectionStatus.value = 'disconnected' })

        try {
            await connection.start()
            connectionStatus.value = 'connected'

            await connection.invoke('JoinLobbyGroup', lobbyId, playerId.value)

            const res = await fetch(`${API_BASE}/api/lobby/${lobbyId}`)
            if (res.ok) {
                lobby.value = await res.json()

                // Mid-game spectator: bootstrap game state if lobby is in_game
                if (lobby.value?.state === 'in_game') {
                    await fetchGameState()
                }
            }
        } catch (err) {
            connectionStatus.value = 'error'
            console.error('SignalR connection failed:', err)
        }
    }

    async function disconnect() {
        if (connection?.state === HubConnectionState.Connected) {
            await connection.invoke('LeaveLobbyGroup', lobbyId, playerId.value)
            await connection.stop()
        }
    }

    // Actions
    async function joinLobby(playerName: string) {
        const res = await fetch(`${API_BASE}/api/lobby/${lobbyId}/join`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerName, platform: 'browser' })
        })
        if (res.ok) {
            const data = await res.json()
            playerId.value = data.playerId
            lobby.value = data.lobby

            // Re-join SignalR group with playerId so backend tracks the connection
            if (connection?.state === HubConnectionState.Connected) {
                await connection.invoke('JoinLobbyGroup', lobbyId, playerId.value)
            }
        }
    }

    async function setReady(ready: boolean) {
        isReady.value = ready
        await fetch(`${API_BASE}/api/lobby/${lobbyId}/ready`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: playerId.value, isReady: ready })
        })
    }

    function sendTap() {
        fetch(`${API_BASE}/api/game/${lobbyId}/tap`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: playerId.value })
        })
    }

    async function fetchGameState() {
        const res = await fetch(`${API_BASE}/api/game/${lobbyId}`)
        if (res.ok) {
            const data = await res.json()
            gameState.value = { players: data.players, timeRemaining: data.timeRemaining }
            triggerRef(gameState)

            if (data.state === 'countdown') {
                gamePhase.value = 'countdown'
            } else if (data.state === 'playing') {
                gamePhase.value = 'playing'
            } else if (data.state === 'finished') {
                gamePhase.value = 'results'
            }
        }
    }

    function resetToLobby() {
        gamePhase.value = 'lobby'
        gameState.value = null
        gameResults.value = null
        countdown.value = 0
        isReady.value = false
        playerId.value = null
    }

    onMounted(connect)
    onUnmounted(disconnect)

    return {
        lobby,
        connectionStatus,
        events,
        gamePhase,
        gameState,
        countdown,
        gameResults,
        playerId,
        isReady,
        joinLobby,
        setReady,
        sendTap,
        fetchGameState,
        resetToLobby,
    }
}
