<script setup lang="ts">
import type { GamePlayer } from '@/types'

const props = defineProps<{
  player: GamePlayer
  isLocal: boolean
}>()
</script>

<template>
  <div class="player-bar" :class="{ local: isLocal }">
    <div class="bar-container">
      <div
        class="bar-fill"
        :class="{ local: isLocal }"
        :style="{ height: player.value + '%' }"
      />
    </div>
    <div class="bar-label">
      <span class="player-name">
        {{ player.name }}
        <span v-if="isLocal" class="you-tag">(YOU)</span>
      </span>
      <span class="platform-badge">{{ player.platform === 'browser' ? '[WEB]' : '[PC]' }}</span>
      <span class="tap-count">{{ player.tapCount }} taps</span>
    </div>
  </div>
</template>

<style scoped>
.player-bar {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 80px;
  max-width: 160px;
}

.bar-container {
  width: 100%;
  height: 300px;
  background: #1e293b;
  border: 2px solid #334155;
  border-radius: 0.5rem;
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: flex-end;
}

.local .bar-container {
  border-color: #34d399;
}

.bar-fill {
  width: 100%;
  background: #6366f1;
  transition: height 0.05s linear;
  border-radius: 0 0 0.35rem 0.35rem;
}

.bar-fill.local {
  background: #34d399;
}

.bar-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  font-size: 0.8rem;
}

.player-name {
  font-weight: 600;
  font-size: 0.9rem;
}

.you-tag {
  color: #34d399;
  font-size: 0.75rem;
}

.platform-badge {
  color: #94a3b8;
  font-size: 0.7rem;
  font-family: monospace;
}

.tap-count {
  color: #64748b;
  font-size: 0.7rem;
}
</style>
