<script setup lang="ts">
import type { GameOverData } from '@/types'

defineProps<{
  results: GameOverData
  localPlayerId: string | null
}>()

const emit = defineEmits<{
  playAgain: []
  quit: []
}>()
</script>

<template>
  <div class="game-results">
    <h1 class="title">GAME OVER</h1>
    <p class="reason">
      {{ results.reason === 'reached_top' ? 'Someone reached the top!' : 'Time ran out!' }}
    </p>

    <div class="results-list">
      <div
        v-for="(player, index) in results.results"
        :key="player.id"
        class="result-row"
        :class="{
          winner: player.id === results.winnerId,
          local: player.id === localPlayerId
        }"
        :style="{ animationDelay: (index * 0.12) + 's' }"
      >
        <span class="rank">
          <span v-if="player.id === results.winnerId" class="crown">&#x1F451;</span>
          #{{ player.rank }}
        </span>
        <span class="name">{{ player.name }}</span>
        <span class="platform">{{ player.platform === 'browser' ? '\u{1F310}' : '\u{1F5A5}\u{FE0F}' }}</span>
        <span class="value">{{ Math.round(player.value) }}%</span>
        <span class="taps">{{ player.tapCount }} taps</span>
      </div>
    </div>

    <div class="button-row">
      <button class="play-again-button btn-game" @click="emit('playAgain')">Play Again</button>
      <button class="quit-button btn-game" @click="emit('quit')">Quit</button>
    </div>
  </div>
</template>

<style scoped>
.game-results {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  padding: 2rem;
}

.title {
  font-size: 3.5rem;
  font-weight: 900;
  color: var(--text-primary);
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-shadow: 0 0 40px var(--glow-primary);
  animation: title-entrance 0.6s ease-out;
}

.reason {
  color: var(--text-secondary);
  font-size: 1.1rem;
  margin: 0;
  font-weight: 600;
}

.results-list {
  width: 100%;
  max-width: 520px;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.result-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.25rem;
  background: var(--bg-card);
  border: 3px solid var(--border-default);
  border-radius: 12px;
  animation: slide-in-row 0.4s ease-out both;
}

.result-row.winner {
  border-color: var(--accent-winner);
  background: #1a1a0e;
  animation: slide-in-row 0.4s ease-out both, winner-glow 1.5s ease-in-out 0.6s infinite;
}

.result-row.local {
  border-color: var(--accent-local);
  background: var(--accent-local-dim);
}

.result-row.winner.local {
  border-color: var(--accent-winner);
  background: #1a2e1a;
}

.rank {
  font-weight: 800;
  font-size: 1.2rem;
  width: 3.5rem;
  color: var(--text-primary);
}

.crown {
  margin-right: 0.15rem;
}

.result-row.winner .rank {
  color: var(--accent-winner);
}

.name {
  font-weight: 700;
  flex: 1;
  color: var(--text-primary);
}

.platform {
  font-size: 1rem;
}

.value {
  font-weight: 700;
  color: var(--accent-primary);
  width: 3.5rem;
  text-align: right;
}

.taps {
  color: var(--text-muted);
  font-size: 0.8rem;
  width: 5rem;
  text-align: right;
}

.button-row {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.play-again-button {
  background: var(--accent-primary);
  color: white;
  padding: 0.85rem 2.5rem;
}

.play-again-button:hover {
  background: var(--accent-primary-hover);
}

.quit-button {
  background: var(--bg-card);
  color: var(--text-secondary);
  border-color: var(--border-default);
  padding: 0.85rem 2.5rem;
}

.quit-button:hover {
  background: var(--bg-card-hover);
  border-color: var(--border-hover);
  box-shadow: none;
}
</style>
