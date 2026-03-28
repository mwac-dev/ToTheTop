import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr"
import { onMounted, onUnmounted, ref } from "vue"

// types to be received from server
export interface Player {
    id: string
    name: string
    isReady: boolean
    joinedAt: string
}

export interface Lobby {
    id: string
    name: string
    players: Player[]
    maxPlayers: number
    state: string
}

export function useLobbyConnection(lobbyId: string) {
    // reactive state for lobby data and connection status
    const lobby = ref<Lobby | null>(null)
    const connectionStatus = ref<string>('disconnected')
    const events = ref<Array<{ type: string; data: AnalyserNode; time: Date }>>([])

    let connection: HubConnection | null = null

    function addEvent(type: string, data: any) {
        events.value.unshift({ type, data, time: new Date() })
        if (events.value.length > 50) events.value.pop() // keep only the latest 50 events
    }

    async function connect() {
        // building the SignalR connection
        connection = new HubConnectionBuilder()
            .withUrl('http://localhost:5082/hub/lobby')
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build()

        // register handlers for server-sent events
        // matching the SendAsync calls in the C# LobbyHub
        connection.on('PlayerJoined', (data: { player: Player; lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('PlayerJoined', data.player)
        })

        connection.on('PlayerReady', (data: { playerId: string; isReady: boolean; lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('PlayerReady', { playerId: data.playerId, isReady: data.isReady })
        })

        connection.on('GameStarting', (data: { lobby: Lobby }) => {
            lobby.value = data.lobby
            addEvent('GameStarting', { players: data.lobby.players })
        })

        connection.onreconnecting(() => { connectionStatus.value = 'reconnecting' })
        connection.onreconnected(() => { connectionStatus.value = 'connected' })
        connection.onclose(() => { connectionStatus.value = 'disconnected' })

        try {
            await connection.start()
            connectionStatus.value = 'connected'

            // join signalR group for the lobby so that events are only received for *this* lobby
            await connection.invoke('JoinLobbyGroup', lobbyId)

            // fetch initial state after connecting
            const res = await fetch(`http://localhost:5082/api/lobby/${lobbyId}`)
            if (res.ok) {
                lobby.value = await res.json()
            }
        } catch (err) {
            connectionStatus.value = 'error'
            console.error('SignalR connection failed:', err)
        }
    }

    async function disconnect() {
        if (connection?.state === HubConnectionState.Connected) {
            await connection.invoke('LeaveLobbyGroup', lobbyId)
            await connection.stop()
        }
    }

    onMounted(connect)
    onUnmounted(disconnect)

    return { lobby, connectionStatus, events }
}