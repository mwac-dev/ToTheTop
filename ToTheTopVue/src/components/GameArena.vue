<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue'
import type { GameState, GamePhase } from '@/types'
import PlayerBar from './PlayerBar.vue'

const HANDLE_EMOJIS = ['\u{1F984}', '\u{1F916}', '\u{1F9CC}', '\u{1F47D}'] // unicorn, robot, troll, alien

const props = defineProps<{
  gameState: GameState | null
  localPlayerId: string | null
  countdown: number
  gamePhase: GamePhase
}>()

const emit = defineEmits<{
  tap: []
}>()

const tapTrigger = ref(false)
const showFlash = ref(false)

const timerUrgency = computed(() => {
  if (!props.gameState) return ''
  const t = props.gameState.timeRemaining
  if (t <= 5) return 'timer-critical'
  if (t <= 10) return 'timer-urgent'
  return ''
})

const isGo = computed(() => props.countdown <= 0)

function handleTap() {
  emit('tap')
  tapTrigger.value = true
  requestAnimationFrame(() => {
    tapTrigger.value = false
  })
}

function handleKeydown(e: KeyboardEvent) {
  if (e.code === 'Space' && props.localPlayerId && props.gamePhase === 'playing') {
    e.preventDefault()
    handleTap()
  }
}

function triggerFlash() {
  showFlash.value = true
  setTimeout(() => { showFlash.value = false }, 400)
}

let lastCountdown = -1
function checkCountdownChange() {
  if (props.countdown <= 0 && lastCountdown > 0) {
    triggerFlash()
  }
  lastCountdown = props.countdown
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  const interval = setInterval(checkCountdownChange, 50)
  onUnmounted(() => clearInterval(interval))
})
onUnmounted(() => window.removeEventListener('keydown', handleKeydown))
</script>

<template>
  <div class="game-arena">
    <div v-if="showFlash" class="screen-flash" />

    <div class="timer" :class="timerUrgency" v-if="gameState && gamePhase === 'playing'">
      {{ Math.ceil(gameState.timeRemaining) }}s
    </div>

    <div class="countdown-overlay" v-if="gamePhase === 'countdown'">
      <div class="countdown-number" :class="{ go: isGo }" :key="countdown">
        {{ countdown > 0 ? countdown : 'GO!' }}
      </div>
    </div>

    <div class="spectating-badge" v-if="!localPlayerId">
      SPECTATING
    </div>

    <div class="bars-row" v-if="gameState">
      <PlayerBar
        v-for="(player, index) in gameState.players"
        :key="player.id"
        :player="player"
        :is-local="player.id === localPlayerId"
        :handle-emoji="HANDLE_EMOJIS[index % HANDLE_EMOJIS.length]!"
      />
    </div>

    <button
      v-if="localPlayerId && gamePhase === 'playing'"
      class="tap-button"
      :class="{ tapped: tapTrigger }"
      @mousedown.prevent="handleTap"
    >
      TAP!
      <span class="tap-hint">(or press Space)</span>
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

.screen-flash {
  position: fixed;
  inset: 0;
  background: white;
  z-index: 200;
  pointer-events: none;
  animation: flash-out 0.4s ease-out forwards;
}

.timer {
  font-size: 2.2rem;
  font-weight: 800;
  color: var(--text-primary);
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.02em;
  transition: color 0.3s;
}

.timer.timer-urgent {
  color: var(--accent-danger);
}

.timer.timer-critical {
  color: var(--accent-danger);
  animation: pulse-danger 0.5s ease-in-out infinite;
}

.countdown-overlay {
  position: fixed;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(10, 14, 26, 0.85);
  z-index: 100;
  pointer-events: none;
}

.countdown-number {
  font-size: 9rem;
  font-weight: 900;
  color: var(--text-primary);
  text-shadow: 0 0 60px var(--glow-primary), 0 0 120px rgba(255, 107, 43, 0.2);
  animation: countdown-pop 0.5s ease-out;
}

.countdown-number.go {
  color: var(--accent-primary);
  text-shadow: 0 0 80px var(--glow-primary), 0 0 160px rgba(255, 107, 43, 0.3);
}

.spectating-badge {
  padding: 0.5rem 1.5rem;
  background: var(--bg-card);
  border: 2px solid var(--border-default);
  border-radius: 9999px;
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-secondary);
  letter-spacing: 0.15em;
  text-transform: uppercase;
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
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
  padding: 1.5rem 5rem;
  font-size: 2rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  background: var(--accent-primary);
  color: white;
  border: 3px solid rgba(255, 255, 255, 0.15);
  border-radius: 16px;
  cursor: pointer;
  user-select: none;
  transition: box-shadow 0.2s, background 0.1s;
  box-shadow: 0 0 0 0 var(--glow-primary);
}

.tap-button:hover {
  box-shadow: 0 0 25px 5px var(--glow-primary);
  background: var(--accent-primary-hover);
}

.tap-button:active,
.tap-button.tapped {
  animation: button-squish 0.2s ease-out;
  background: var(--accent-primary-active);
}

.tap-hint {
  font-size: 0.7rem;
  font-weight: 500;
  opacity: 0.6;
  letter-spacing: 0.02em;
  text-transform: none;
}
</style>
