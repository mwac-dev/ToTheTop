<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLobbyConnection } from '@/composables/useLobbyConnection'
import GameArena from '@/components/GameArena.vue'
import GameResults from '@/components/GameResults.vue'

const route = useRoute()
const router = useRouter()
const lobbyId = route.params.id as string

const {
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
  resetToLobby,
} = useLobbyConnection(lobbyId)

const joinName = ref('')

async function handleJoin() {
  if (!joinName.value.trim()) return
  await joinLobby(joinName.value.trim())
  joinName.value = ''
}

async function handleReady() {
  await setReady(!isReady.value)
}

function handleTap() {
  sendTap()
}

function handleBack() {
  resetToLobby()
  router.push('/')
}
</script>

<template>
  <div class="lobby-view">
    <!-- Connection status indicator -->
    <div class="status-bar" :class="connectionStatus">
      <span class="status-dot" />
      {{ connectionStatus }}
    </div>

    <!-- Loading state -->
    <div v-if="!lobby" class="loading">
      Connecting to lobby...
    </div>

    <!-- GAME RESULTS PHASE -->
    <GameResults
      v-else-if="gamePhase === 'results' && gameResults"
      :results="gameResults"
      :local-player-id="playerId"
      @back="handleBack"
    />

    <!-- COUNTDOWN / PLAYING PHASE -->
    <GameArena
      v-else-if="gamePhase === 'countdown' || gamePhase === 'playing'"
      :game-state="gameState"
      :local-player-id="playerId"
      :countdown="countdown"
      :game-phase="gamePhase"
      @tap="handleTap"
    />

    <!-- LOBBY PHASE -->
    <template v-else>
      <header>
        <h1>{{ lobby.name }}</h1>
        <span class="badge" :class="lobby.state">{{ lobby.state }}</span>
      </header>

      <!-- Join form -->
      <section
        v-if="!playerId && !lobby.isFull && lobby.state === 'waiting'"
        class="join-section"
      >
        <form @submit.prevent="handleJoin" class="join-form">
          <input
            v-model="joinName"
            type="text"
            placeholder="Enter your name..."
            class="join-input"
            maxlength="20"
          />
          <button type="submit" class="join-button" :disabled="!joinName.trim()">
            Join Lobby
          </button>
        </form>
      </section>

      <!-- Ready button -->
      <section v-if="playerId && lobby.state === 'waiting'" class="ready-section">
        <button
          class="ready-button"
          :class="{ active: isReady }"
          @click="handleReady"
        >
          {{ isReady ? 'READY!' : 'Click to Ready Up' }}
        </button>
      </section>

      <!-- Player list -->
      <section class="players">
        <h2>Players ({{ lobby.players.length }}/{{ lobby.maxPlayers }})</h2>
        <div class="player-grid">
          <div
            v-for="player in lobby.players"
            :key="player.id"
            class="player-card"
            :class="{ ready: player.isReady, self: player.id === playerId }"
          >
            <div class="player-name">
              {{ player.name }}
              <span v-if="player.id === playerId" class="you-tag">(YOU)</span>
            </div>
            <div class="player-status">
              {{ player.isReady ? 'READY' : 'Not Ready' }}
            </div>
          </div>

          <!-- Empty slots -->
          <div
            v-for="i in (lobby.maxPlayers - lobby.players.length)"
            :key="'empty-' + i"
            class="player-card empty"
          >
            <div class="player-name">Waiting...</div>
          </div>
        </div>
      </section>

      <!-- Live event feed -->
      <section class="events">
        <h2>Live Events</h2>
        <div class="event-list">
          <div v-for="(event, index) in events" :key="index" class="event-item">
            <span class="event-time">
              {{ event.time.toLocaleTimeString() }}
            </span>
            <span class="event-type">{{ event.type }}</span>
            <span class="event-data">{{ JSON.stringify(event.data) }}</span>
          </div>
          <div v-if="events.length === 0" class="no-events">
            No events yet — join from Unity or use the form above
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.lobby-view {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
  font-family: system-ui, sans-serif;
}

.status-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  margin-bottom: 1.5rem;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.status-bar.connected { background: #065f46; color: #a7f3d0; }
.status-bar.connected .status-dot { background: #34d399; }
.status-bar.disconnected { background: #7f1d1d; color: #fca5a5; }
.status-bar.disconnected .status-dot { background: #f87171; }
.status-bar.reconnecting { background: #78350f; color: #fcd34d; }
.status-bar.reconnecting .status-dot { background: #fbbf24; }
.status-bar.error { background: #7f1d1d; color: #fca5a5; }
.status-bar.error .status-dot { background: #f87171; }

header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 2rem;
}

h1 { margin: 0; }

.badge {
  padding: 0.25rem 0.75rem;
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
}
.badge.waiting { background: #1e3a5f; color: #93c5fd; }
.badge.in_game { background: #065f46; color: #a7f3d0; }

/* Join form */
.join-section {
  margin-bottom: 1.5rem;
}

.join-form {
  display: flex;
  gap: 0.75rem;
}

.join-input {
  flex: 1;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  border: 2px solid #334155;
  background: #1e293b;
  color: #f1f5f9;
  font-size: 1rem;
  outline: none;
}

.join-input:focus {
  border-color: #6366f1;
}

.join-button {
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  border: none;
  background: #6366f1;
  color: white;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
}

.join-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.join-button:not(:disabled):hover {
  background: #4f46e5;
}

/* Ready button */
.ready-section {
  margin-bottom: 1.5rem;
}

.ready-button {
  width: 100%;
  padding: 1rem;
  border-radius: 0.75rem;
  border: 2px solid #334155;
  background: #1e293b;
  color: #94a3b8;
  font-size: 1.1rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.ready-button.active {
  border-color: #34d399;
  background: #064e3b;
  color: #34d399;
}

.ready-button:hover {
  border-color: #6366f1;
}

.player-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
  margin-top: 1rem;
}

.player-card {
  padding: 1.25rem;
  border-radius: 0.75rem;
  border: 2px solid #334155;
  background: #1e293b;
  transition: all 0.3s ease;
}

.player-card.ready {
  border-color: #34d399;
  background: #064e3b;
}

.player-card.self {
  box-shadow: 0 0 0 1px #6366f1;
}

.player-card.empty {
  border-style: dashed;
  opacity: 0.4;
}

.player-name {
  font-weight: 600;
  font-size: 1.1rem;
}

.you-tag {
  color: #34d399;
  font-size: 0.8rem;
}

.player-status {
  margin-top: 0.5rem;
  font-size: 0.875rem;
  opacity: 0.7;
}

.events {
  margin-top: 2rem;
}

.event-list {
  margin-top: 0.5rem;
  font-family: monospace;
  font-size: 0.8rem;
  max-height: 300px;
  overflow-y: auto;
  background: #0f172a;
  border-radius: 0.5rem;
  padding: 1rem;
}

.event-item {
  padding: 0.25rem 0;
  display: flex;
  gap: 1rem;
}

.event-time { color: #64748b; }
.event-type { color: #a78bfa; font-weight: 600; }
.event-data { color: #94a3b8; }

.no-events { color: #64748b; font-style: italic; }

.loading {
  text-align: center;
  padding: 4rem;
  color: #94a3b8;
}
</style>
