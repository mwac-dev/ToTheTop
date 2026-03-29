<script setup lang="ts">
import { computed } from 'vue'
import type { GamePlayer } from '@/types'

const props = defineProps<{
  player: GamePlayer
  isLocal: boolean
  handleEmoji: string
}>()

const shakeClass = computed(() => {
  const v = props.player.value
  if (v >= 90) return 'shake-intense'
  if (v >= 75) return 'shake-moderate'
  if (v >= 50) return 'shake-subtle'
  return ''
})

const stripeSpeedClass = computed(() => {
  const v = props.player.value
  if (v >= 90) return 'stripe-fast'
  if (v >= 50) return 'stripe-medium'
  return ''
})
</script>

<template>
  <div class="player-bar" :class="{ local: isLocal }">
    <span class="handle-icon">{{ handleEmoji }}</span>
    <div class="bar-container" :class="[shakeClass, { glowing: player.value >= 90 }]">
      <div
        class="bar-fill"
        :class="[{ local: isLocal }, stripeSpeedClass]"
        :style="{ height: player.value + '%' }"
      />
    </div>
    <div class="bar-label">
      <span class="player-name">{{ player.name }}</span>
      <span class="platform-badge">{{ player.platform === 'browser' ? '\u{1F310}' : '\u{1F5A5}\u{FE0F}' }}</span>
      <span class="tap-count">{{ player.tapCount }} taps</span>
    </div>
  </div>
</template>

<style scoped>
.player-bar {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  min-width: 60px;
  max-width: 100px;
}

.handle-icon {
  font-size: 1.4rem;
  line-height: 1;
}

.bar-container {
  width: 40px;
  height: 300px;
  background: var(--bg-track);
  border: 3px solid var(--border-default);
  border-radius: 10px;
  position: relative;
  overflow: hidden;
  display: flex;
  align-items: flex-end;
  transition: box-shadow 0.3s, border-color 0.3s;
}

.local .bar-container {
  border-color: var(--accent-local);
}

.bar-container.shake-subtle {
  animation: shake-subtle 0.2s linear infinite;
}
.bar-container.shake-moderate {
  animation: shake-moderate 0.15s linear infinite;
}
.bar-container.shake-intense {
  animation: shake-intense 0.12s linear infinite;
}

.bar-container.glowing {
  box-shadow: 0 0 20px 4px var(--glow-other);
  border-color: var(--accent-other);
}
.local .bar-container.glowing {
  box-shadow: 0 0 20px 4px var(--glow-local);
  border-color: var(--accent-local);
}

.bar-fill {
  width: 100%;
  background: var(--accent-other);
  transition: height 0.05s linear;
  border-radius: 0 0 7px 7px;
  position: relative;
}

.bar-fill.local {
  background: var(--accent-local);
}

.bar-fill::after {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(
    -45deg,
    transparent,
    transparent 6px,
    var(--stripe-color) 6px,
    var(--stripe-color) 12px
  );
  animation: stripe-scroll 0.8s linear infinite;
  pointer-events: none;
}

.bar-fill.stripe-medium::after {
  animation-duration: 0.5s;
}

.bar-fill.stripe-fast::after {
  animation-duration: 0.25s;
}

.bar-label {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  font-size: 0.8rem;
}

.player-name {
  font-weight: 700;
  font-size: 0.9rem;
  color: var(--text-primary);
}

.platform-badge {
  font-size: 0.85rem;
  line-height: 1;
}

.tap-count {
  color: var(--text-muted);
  font-size: 0.7rem;
}
</style>
