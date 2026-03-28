<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useLobbyStore } from '@/stores/lobby'

const store = useLobbyStore()
const router = useRouter()
const newLobbyName = ref('')

onMounted(() => {
  store.fetchLobbies()
})

async function handleCreate() {
  if (!newLobbyName.value.trim()) return
  const lobby = await store.createLobby(newLobbyName.value)
  newLobbyName.value = ''
  router.push(`/lobby/${lobby.id}`)
}
</script>

<template>
  <div class="home">
    <h1>Game Lobbies</h1>

    <!-- Create lobby form -->
    <div class="create-form">
      <input
        v-model="newLobbyName"
        placeholder="Lobby name..."
        @keyup.enter="handleCreate"
      />
      <button @click="handleCreate">Create Lobby</button>
    </div>

    <!-- Lobby list -->
    <div v-if="store.loading" class="loading">Loading...</div>

    <div v-else-if="store.lobbies.length === 0" class="empty">
      No lobbies yet. Create one!
    </div>

    <div v-else class="lobby-list">
      <router-link
        v-for="lobby in store.lobbies"
        :key="lobby.id"
        :to="`/lobby/${lobby.id}`"
        class="lobby-item"
      >
        <div class="lobby-name">{{ lobby.name }}</div>
        <div class="lobby-info">
          {{ lobby.players.length }}/{{ lobby.maxPlayers }} players
          · {{ lobby.state }}
        </div>
      </router-link>
    </div>
  </div>
</template>

<style scoped>
.home {
  max-width: 600px;
  margin: 0 auto;
  padding: 2rem;
  font-family: system-ui, sans-serif;
}

.create-form {
  display: flex;
  gap: 0.5rem;
  margin: 1.5rem 0;
}

.create-form input {
  flex: 1;
  padding: 0.75rem;
  border-radius: 0.5rem;
  border: 1px solid #334155;
  background: #1e293b;
  color: #f1f5f9;
  font-size: 1rem;
}

.create-form button {
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  border: none;
  background: #6366f1;
  color: white;
  font-weight: 600;
  cursor: pointer;
}

.create-form button:hover { background: #4f46e5; }

.lobby-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.lobby-item {
  display: block;
  padding: 1rem 1.25rem;
  border-radius: 0.5rem;
  background: #1e293b;
  color: #f1f5f9;
  text-decoration: none;
  border: 1px solid #334155;
  transition: border-color 0.2s;
}

.lobby-item:hover { border-color: #6366f1; }

.lobby-name { font-weight: 600; }
.lobby-info { font-size: 0.875rem; color: #94a3b8; margin-top: 0.25rem; }

.loading, .empty {
  text-align: center;
  padding: 3rem;
  color: #94a3b8;
}
</style>