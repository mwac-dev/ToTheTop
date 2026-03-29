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
        v-for="player in results.results"
        :key="player.id"
        class="result-row"
        :class="{
          winner: player.id === results.winnerId,
          local: player.id === localPlayerId
        }"
      >
        <span class="rank">#{{ player.rank }}</span>
        <span class="name">{{ player.name }}</span>
        <span class="platform">{{ player.platform === 'browser' ? '[WEB]' : '[PC]' }}</span>
        <span class="value">{{ Math.round(player.value) }}%</span>
        <span class="taps">{{ player.tapCount }} taps</span>
      </div>
    </div>

    <div class="button-row">
      <button class="play-again-button" @click="emit('playAgain')">Play Again</button>
      <button class="quit-button" @click="emit('quit')">Quit</button>
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
  font-size: 3rem;
  font-weight: 900;
  color: #f1f5f9;
  margin: 0;
}

.reason {
  color: #94a3b8;
  font-size: 1.1rem;
  margin: 0;
}

.results-list {
  width: 100%;
  max-width: 500px;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.result-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.25rem;
  background: #1e293b;
  border: 2px solid #334155;
  border-radius: 0.75rem;
}

.result-row.winner {
  border-color: #eab308;
  background: #1a1a0e;
}

.result-row.local {
  border-color: #34d399;
  background: #064e3b;
}

.result-row.winner.local {
  border-color: #eab308;
  background: #1a2e1a;
}

.rank {
  font-weight: 700;
  font-size: 1.2rem;
  width: 2.5rem;
  color: #e2e8f0;
}

.result-row.winner .rank {
  color: #eab308;
}

.name {
  font-weight: 600;
  flex: 1;
}

.platform {
  color: #94a3b8;
  font-family: monospace;
  font-size: 0.8rem;
}

.value {
  font-weight: 600;
  color: #a78bfa;
  width: 3.5rem;
  text-align: right;
}

.taps {
  color: #64748b;
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
  padding: 0.75rem 2rem;
  font-size: 1rem;
  font-weight: 600;
  background: #6366f1;
  color: #f1f5f9;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.play-again-button:hover {
  background: #4f46e5;
}

.quit-button {
  padding: 0.75rem 2rem;
  font-size: 1rem;
  font-weight: 600;
  background: #334155;
  color: #f1f5f9;
  border: none;
  border-radius: 0.5rem;
  cursor: pointer;
}

.quit-button:hover {
  background: #475569;
}
</style>
