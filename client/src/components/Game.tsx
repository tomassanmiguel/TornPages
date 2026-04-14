import { useGame } from '../hooks/useGame';
import { LeftPage } from './LeftPage';
import { RightPage } from './RightPage';
import styles from './Game.module.css';

interface Props {
  profileId: string;
  onExit: () => void;
}

export function Game({ profileId, onExit }: Props) {
  const { state, status, error, dispatch, isLoading, historyIndex, viewHistory, returnToCurrent } = useGame(profileId);

  if (status === 'idle' || (status === 'loading' && !state)) {
    return (
      <div className={styles.loading}>
        <div className={styles.loadingSpinner} />
        <p>Loading…</p>
      </div>
    );
  }

  if (!state) {
    return (
      <div className={styles.error}>
        <p>Failed to load game state</p>
        {error && <p className={styles.errorDetail}>{error}</p>}
        <button onClick={onExit}>← Menu</button>
      </div>
    );
  }

  const isHistory = historyIndex !== null;

  return (
    <div className={styles.shell}>
      {/* Top bar */}
      <div className={styles.topBar}>
        <button className={styles.exitBtn} onClick={onExit}>← Menu</button>
        <div className={styles.topCenter}>
          {isHistory && (
            <span className={styles.historyBadge}>
              Viewing page {historyIndex + 1} of {state.totalPages}
              <button className={styles.returnBtn} onClick={returnToCurrent}>Return to current</button>
            </span>
          )}
          {!isHistory && isLoading && <span className={styles.spinnerSmall} />}
          {error && <span className={styles.errorBadge}>{error}</span>}
        </div>
        <div className={styles.topRight}>
          {/* Page navigation */}
          {state.totalPages > 1 && !isHistory && (
            <div className={styles.pageNav}>
              {Array.from({ length: Math.min(state.totalPages, 15) }, (_, i) => {
                const pageNum = state.totalPages > 15
                  ? i + (state.totalPages - 15)
                  : i;
                const isCurrent = pageNum === state.currentPageIndex;
                return (
                  <button
                    key={pageNum}
                    className={`${styles.pageNavDot} ${isCurrent ? styles.pageNavDotActive : ''}`}
                    onClick={() => viewHistory(pageNum)}
                    title={`Page ${pageNum + 1}`}
                  />
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Book spread */}
      <div className={styles.spread}>
        <div className={styles.leftPage}>
          <LeftPage left={state.left} right={state.right} dispatch={dispatch} isLoading={isLoading} />
        </div>
        <div className={styles.spine} />
        <div className={styles.rightPage}>
          <RightPage
            state={state}
            dispatch={dispatch}
            isLoading={isLoading}
            isHistory={isHistory}
          />
        </div>
      </div>
    </div>
  );
}
