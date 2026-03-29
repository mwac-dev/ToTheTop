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
    <h1 class="game-title">TO THE TOP</h1>

    <div class="create-form">
      <input
        v-model="newLobbyName"
        placeholder="Lobby name..."
        @keyup.enter="handleCreate"
      />
      <button class="btn-game create-btn" @click="handleCreate">Create Lobby</button>
    </div>

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
          &middot; {{ lobby.state }}
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
}

.game-title {
  font-size: 3rem;
  font-weight: 900;
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: var(--text-primary);
  text-shadow: 0 0 40px var(--glow-primary), 0 0 80px rgba(255, 107, 43, 0.15);
  margin-bottom: 0.5rem;
}

.create-form {
  display: flex;
  gap: 0.75rem;
  margin: 1.5rem 0;
}

.create-form input {
  flex: 1;
  padding: 0.75rem 1rem;
  border-radius: 10px;
  border: 3px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-primary);
  font-size: 1rem;
  font-weight: 600;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.create-form input:focus {
  border-color: var(--accent-primary);
  box-shadow: 0 0 15px 2px var(--glow-primary);
}

.create-btn {
  background: var(--accent-primary);
  color: white;
}

.create-btn:hover {
  background: var(--accent-primary-hover);
}

.lobby-list {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.lobby-item {
  display: block;
  padding: 1rem 1.25rem;
  border-radius: 10px;
  background: var(--bg-card);
  color: var(--text-primary);
  text-decoration: none;
  border: 3px solid var(--border-default);
  border-left: 5px solid var(--border-default);
  transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;
}

.lobby-item:hover {
  border-color: var(--accent-primary);
  border-left-color: var(--accent-primary);
  background: var(--bg-card-hover);
  box-shadow: 0 0 12px 1px var(--glow-primary);
}

.lobby-name {
  font-weight: 700;
  font-size: 1.05rem;
}

.lobby-info {
  font-size: 0.85rem;
  color: var(--text-secondary);
  margin-top: 0.25rem;
}

.loading, .empty {
  text-align: center;
  padding: 3rem;
  color: var(--text-muted);
  font-weight: 600;
}
</style>
