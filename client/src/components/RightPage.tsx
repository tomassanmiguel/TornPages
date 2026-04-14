import type { RenderState, PlayerAction } from '../api/types';
import { PrologueCrewPage } from './pages/PrologueCrew';
import { PrologueBoonPage } from './pages/PrologueBoon';
import { LocationSelectPage } from './pages/LocationSelect';
import { BuildUpPage } from './pages/BuildUp';
import { EventPage } from './pages/EventPage';
import { EnemySelectPage } from './pages/EnemySelect';
import { CombatPage } from './pages/CombatPage';
import { CombatRewardPage } from './pages/CombatReward';
import { ChapterConclusionPage } from './pages/ChapterConclusion';
import { EpiloguePage } from './pages/Epilogue';
import styles from './RightPage.module.css';

interface Props {
  state: RenderState;
  dispatch: (action: PlayerAction) => void;
  isLoading: boolean;
  isHistory: boolean;
}

export function RightPage({ state, dispatch, isLoading, isHistory }: Props) {
  const right = state.right;
  const disabled = isLoading || isHistory;

  const content = (() => {
    switch (right.$type) {
      case 'PrologueCrew':
        return <PrologueCrewPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'PrologueBoon':
        return <PrologueBoonPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'LocationSelect':
        return <LocationSelectPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'BuildUp':
        return <BuildUpPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'Event':
        return <EventPage page={right} left={state.left} dispatch={dispatch} disabled={disabled} />;
      case 'EnemySelect':
        return <EnemySelectPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'Combat':
        return <CombatPage page={right} left={state.left} dispatch={dispatch} disabled={disabled} />;
      case 'CombatReward':
        return <CombatRewardPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'ChapterConclusion':
        return <ChapterConclusionPage page={right} dispatch={dispatch} disabled={disabled} />;
      case 'Epilogue':
        return <EpiloguePage page={right} dispatch={dispatch} disabled={disabled} />;
      default:
        return <div className={styles.unknown}>Unknown page type: {(right as { $type: string }).$type}</div>;
    }
  })();

  return (
    <div className={styles.rightPage}>
      {isHistory && (
        <div className={styles.historyOverlay}>Viewing history — actions disabled</div>
      )}
      {content}
    </div>
  );
}
