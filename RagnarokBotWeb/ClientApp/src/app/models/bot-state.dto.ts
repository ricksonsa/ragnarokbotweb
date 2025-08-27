export interface BotState {
  botId: string
  steamId: string
  connected: boolean
  gameActive: boolean
  lastSeen: string
  minutesSinceLastSeen: number
}