import { ref, computed } from 'vue'
import { defineStore } from 'pinia'

const API_BASE = 'http://localhost:5082/api/lobby'

export interface LobbyListItem {
  id: string
  name: string
  players: Array<{ id: string; name: string; isReady: boolean }>
  maxPlayers: number
  state: string
}

export const useLobbyStore = defineStore('lobby', () => {
  const lobbies = ref<LobbyListItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  //getters  
  const availableLobbies = computed(() =>
    lobbies.value.filter(l => l.state === 'waiting' && l.players.length < l.maxPlayers)
  )
  const lobbyCount = computed(() => lobbies.value.length)

  // Actions / async functions
  async function fetchLobbies() {
    loading.value = true
    error.value = null
    try {
      const res = await fetch(API_BASE)
      lobbies.value = await res.json()
    } catch (err) {
      error.value = 'Failed to load lobbies'
    } finally {
      loading.value = false
    }
  }

  async function createLobby(name: string, maxPlayers: number = 4) {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, maxPlayers })
    })
    const lobby = await res.json()
    lobbies.value.push(lobby)
    return lobby
  }

  return { lobbies, loading, error, availableLobbies, lobbyCount, fetchLobbies, createLobby }
})