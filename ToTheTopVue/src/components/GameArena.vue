<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import type { GameState, GamePhase } from '@/types'
import PlayerBar from './PlayerBar.vue'

const props = defineProps<{
  gameState: GameState | null
  localPlayerId: string | null
  countdown: number
  gamePhase: GamePhase
}>()

const emit = defineEmits<{
  tap: []
}>()

function handleKeydown(e: KeyboardEvent) {
  if (e.code === 'Space' && props.localPlayerId && props.gamePhase === 'playing') {
    e.preventDefault()
    emit('tap')
  }
}

onMounted(() => window.addEventListener('keydown', handleKeydown))
onUnmounted(() => window.removeEventListener('keydown', handleKeydown))
</script>

<template>
  <div class="game-arena">
    <!-- Timer -->
    <div class="timer" v-if="gameState && gamePhase === 'playing'">
      {{ Math.ceil(gameState.timeRemaining) }}s
    </div>

    <!-- Countdown overlay -->
    <div class="countdown-overlay" v-if="gamePhase === 'countdown'">
      <div class="countdown-number">{{ countdown > 0 ? countdown : 'GO!' }}</div>
    </div>

    <!-- Spectating badge -->
    <div class="spectating-badge" v-if="!localPlayerId">
      SPECTATING
    </div>

    <!-- Player bars -->
    <div class="bars-row" v-if="gameState">
      <PlayerBar
        v-for="player in gameState.players"
        :key="player.id"
        :player="player"
        :is-local="player.id === localPlayerId"
      />
    </div>

    <!-- Tap button (players only) -->
    <button
      v-if="localPlayerId && gamePhase === 'playing'"
      class="tap-button"
      @mousedown.prevent="emit('tap')"
    >
      TAP! (or press Space)
    </button>
  </div>
</template>

<style scoped>
.game-arena {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  padding: 2rem;
  min-height: 500px;
}

.timer {
  font-size: 2rem;
  font-weight: 700;
  color: #f1f5f9;
  font-variant-numeric: tabular-nums;
}

.countdown-overlay {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.6);
  z-index: 100;
  pointer-events: none;
}

.countdown-number {
  font-size: 8rem;
  font-weight: 900;
  color: #f1f5f9;
  text-shadow: 0 0 40px rgba(99, 102, 241, 0.8);
}

.spectating-badge {
  padding: 0.5rem 1.5rem;
  background: #1e293b;
  border: 1px solid #475569;
  border-radius: 9999px;
  font-size: 0.8rem;
  font-weight: 600;
  color: #94a3b8;
  letter-spacing: 0.1em;
}

.bars-row {
  display: flex;
  gap: 1.5rem;
  justify-content: center;
  align-items: flex-end;
  width: 100%;
  max-width: 700px;
}

.tap-button {
  padding: 1.5rem 4rem;
  font-size: 1.5rem;
  font-weight: 700;
  background: #6366f1;
  color: white;
  border: none;
  border-radius: 1rem;
  cursor: pointer;
  user-select: none;
  transition: transform 0.05s, background 0.1s;
}

.tap-button:active {
  transform: scale(0.95);
  background: #4f46e5;
}
</style>
