
export interface GameKillData {
  killer: {
    serverLocation: {
      x: number;
      y: number;
      z: number;
    };
    clientLocation: {
    x: number;
      y: number;
      z: number;
    };
    isInGameEvent: boolean;
    profileName: string;
    userId: string;
    hasImmortality: boolean;
  };
  victim: {
     serverLocation: {
      x: number;
      y: number;
      z: number;
    };
    clientLocation: {
    x: number;
      y: number;
      z: number;
    };
    isInGameEvent: boolean;
    profileName: string;
    userId: string;
  };
  weapon: string;
  date: string;
  timeOfDay: string;
  line: string;
}