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
  returnToLobby,
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

function handlePlayAgain() {
  returnToLobby()
}

function handleQuit() {
  resetToLobby()
  router.push('/')
}
</script>

<template>
  <div class="lobby-view">
    <div class="status-bar" :class="connectionStatus">
      <span class="status-dot" />
      {{ connectionStatus }}
    </div>

    <div v-if="!lobby" class="loading">
      Connecting to lobby...
    </div>

    <GameResults
      v-else-if="gamePhase === 'results' && gameResults"
      :results="gameResults"
      :local-player-id="playerId"
      @play-again="handlePlayAgain"
      @quit="handleQuit"
    />

    <GameArena
      v-else-if="gamePhase === 'countdown' || gamePhase === 'playing'"
      :game-state="gameState"
      :local-player-id="playerId"
      :countdown="countdown"
      :game-phase="gamePhase"
      @tap="handleTap"
    />

    <template v-else>
      <header>
        <h1 class="lobby-title">{{ lobby.name }}</h1>
        <span class="badge" :class="lobby.state">{{ lobby.state }}</span>
      </header>

      <section
        v-if="!playerId && lobby.players.length < lobby.maxPlayers && lobby.state === 'waiting'"
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
          <button type="submit" class="btn-game join-button" :disabled="!joinName.trim()">
            Join Lobby
          </button>
        </form>
      </section>

      <section v-if="playerId && lobby.state === 'waiting'" class="ready-section">
        <button
          class="ready-button"
          :class="{ active: isReady }"
          @click="handleReady"
        >
          {{ isReady ? 'READY!' : 'Click to Ready Up' }}
        </button>
      </section>

      <section class="players">
        <h2>Players ({{ lobby.players.length }}/{{ lobby.maxPlayers }})</h2>
        <div class="player-grid">
          <div
            v-for="player in lobby.players"
            :key="player.id"
            class="player-card"
            :class="{ ready: player.isReady, self: player.id === playerId }"
          >
            <div class="player-name">{{ player.name }}</div>
            <div class="player-status">
              {{ player.isReady ? 'READY' : 'Not Ready' }}
            </div>
          </div>

          <div
            v-for="i in (lobby.maxPlayers - lobby.players.length)"
            :key="'empty-' + i"
            class="player-card empty"
          >
            <div class="player-name">Waiting...</div>
          </div>
        </div>
      </section>

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
}

.status-bar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 10px;
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 1.5rem;
  border: 2px solid transparent;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.status-bar.connected { background: #062e22; color: var(--accent-local); border-color: #0a3d28; }
.status-bar.connected .status-dot { background: var(--accent-local); box-shadow: 0 0 8px var(--glow-local); }
.status-bar.disconnected { background: #2d0a0a; color: var(--accent-danger); border-color: #3d1414; }
.status-bar.disconnected .status-dot { background: var(--accent-danger); }
.status-bar.reconnecting { background: #2d1f0a; color: #fbbf24; border-color: #3d2a14; }
.status-bar.reconnecting .status-dot { background: #fbbf24; }
.status-bar.error { background: #2d0a0a; color: var(--accent-danger); border-color: #3d1414; }
.status-bar.error .status-dot { background: var(--accent-danger); }

header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 2rem;
}

.lobby-title {
  margin: 0;
  font-size: 1.8rem;
  font-weight: 800;
  color: var(--text-primary);
  letter-spacing: 0.02em;
}

h2 {
  font-weight: 700;
  color: var(--text-primary);
  font-size: 1.1rem;
}

.badge {
  padding: 0.3rem 0.85rem;
  border-radius: 9999px;
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}
.badge.waiting { background: var(--accent-other-dim); color: var(--accent-other); border: 1px solid rgba(77,138,255,0.3); }
.badge.in_game { background: var(--accent-local-dim); color: var(--accent-local); border: 1px solid rgba(0,232,123,0.3); }

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
  border-radius: 10px;
  border: 3px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-primary);
  font-size: 1rem;
  font-weight: 600;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.join-input:focus {
  border-color: var(--accent-primary);
  box-shadow: 0 0 15px 2px var(--glow-primary);
}

.join-button {
  background: var(--accent-primary);
  color: white;
}

.join-button:not(:disabled):hover {
  background: var(--accent-primary-hover);
}

.ready-section {
  margin-bottom: 1.5rem;
}

.ready-button {
  width: 100%;
  padding: 1rem;
  border-radius: 12px;
  border: 3px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-secondary);
  font-size: 1.1rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  cursor: pointer;
  transition: all 0.2s;
}

.ready-button:hover {
  border-color: var(--accent-primary);
  box-shadow: 0 0 15px 2px var(--glow-primary);
}

.ready-button:active {
  animation: button-squish 0.25s ease-out;
}

.ready-button.active {
  border-color: var(--accent-local);
  background: var(--accent-local-dim);
  color: var(--accent-local);
  box-shadow: 0 0 20px 3px var(--glow-local);
}

.player-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
  margin-top: 1rem;
}

.player-card {
  padding: 1.25rem;
  border-radius: 12px;
  border: 3px solid var(--border-default);
  border-left: 5px solid var(--border-default);
  background: var(--bg-card);
  transition: all 0.3s ease;
}

.player-card.ready {
  border-color: var(--accent-local);
  border-left-color: var(--accent-local);
  background: var(--accent-local-dim);
}

.player-card.self {
  box-shadow: 0 0 12px 1px var(--glow-primary);
}

.player-card.empty {
  border-style: dashed;
  opacity: 0.35;
}

.player-name {
  font-weight: 700;
  font-size: 1.1rem;
  color: var(--text-primary);
}

.player-status {
  margin-top: 0.5rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-muted);
}

.player-card.ready .player-status {
  color: var(--accent-local);
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
  background: var(--bg-track);
  border: 2px solid var(--border-default);
  border-radius: 10px;
  padding: 1rem;
}

.event-item {
  padding: 0.25rem 0;
  display: flex;
  gap: 1rem;
}

.event-time { color: var(--text-muted); }
.event-type { color: var(--accent-primary); font-weight: 700; }
.event-data { color: var(--text-secondary); }

.no-events { color: var(--text-muted); font-style: italic; }

.loading {
  text-align: center;
  padding: 4rem;
  color: var(--text-muted);
  font-weight: 600;
}
</style>
