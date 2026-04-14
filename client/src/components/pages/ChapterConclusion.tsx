import type { ChapterConclusionRight, PlayerAction } from '../../api/types';
import { ProceedAction } from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: ChapterConclusionRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function ChapterConclusionPage({ page, dispatch, disabled }: Props) {
  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>Chapter {page.chapterNumber} Complete</div>
      <div className={styles.narrative}>{page.narrativeText}</div>
      {!page.isCommitted_ && (
        <div className={styles.actionRow}>
          <button
            className={styles.primaryBtn}
            onClick={() => dispatch(ProceedAction())}
            disabled={disabled}
          >
            {page.chapterNumber >= 10 ? 'End Game' : 'Next Chapter →'}
          </button>
        </div>
      )}
      {page.isCommitted_ && <div className={styles.committedBadge}>✓ Proceeding…</div>}
    </div>
  );
}
