import type { BuildUpRight, PlayerAction, EventCardRender } from '../../api/types';
import { SelectEventAction, ReadyBuildUpAction, TearAction } from '../../api/types';
import styles from './shared.module.css';
import bStyles from './BuildUp.module.css';

interface Props {
  page: BuildUpRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

const DT_COLORS: Record<number, string> = {
  [-2]: '#70c870', [-1]: '#90c060', [0]: '#a0a060', [1]: '#c09040', [2]: '#c06040', [3]: '#d04040',
  [4]: '#d03030', [5]: '#c02020'
};

export function BuildUpPage({ page, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Build-up</div>
        <div className={styles.committedBadge}>✓ Events queued</div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>Build-Up Phase</div>
      <p className={styles.pageSubtitle}>
        Select events to build Dramatic Tension before the battle. Need at least {page.dtThreshold} DT.
      </p>

      <div className={bStyles.dtBar}>
        <span className={bStyles.dtLabel}>DT:</span>
        <span className={bStyles.dtValue} style={{ color: page.totalDT >= page.dtThreshold ? 'var(--green)' : 'var(--red)' }}>
          {page.totalDT}
        </span>
        <span className={bStyles.dtDivider}>/</span>
        <span className={bStyles.dtThreshold}>{page.dtThreshold}</span>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Available Events</div>
        <div className={styles.optionGrid}>
          {page.eventPool.map(evt => (
            <EventCard
              key={evt.id}
              evt={evt}
              selected={page.selectedEventIds.includes(evt.id)}
              onToggle={() => dispatch(SelectEventAction(evt.id))}
              disabled={disabled}
            />
          ))}
        </div>
      </div>

      {page.selectedEventIds.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Selected ({page.selectedEventIds.length})</div>
          <div className={bStyles.selectedList}>
            {page.selectedEventIds.map(id => {
              const evt = page.eventPool.find(e => e.id === id);
              return evt ? (
                <span key={id} className={bStyles.selectedTag}>
                  {evt.name} ({evt.dramaticTension > 0 ? '+' : ''}{evt.dramaticTension} DT)
                </span>
              ) : null;
            })}
          </div>
        </div>
      )}

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ReadyBuildUpAction())}
          disabled={disabled || !page.canReady}
          title={!page.canReady ? `Need ${page.dtThreshold} DT` : undefined}
        >
          Ready for Combat
        </button>
        <button
          className={styles.secondaryBtn}
          onClick={() => dispatch(TearAction())}
          disabled={disabled}
          title="Regenerate this page with a new event pool"
        >
          Tear Page
        </button>
      </div>
    </div>
  );
}

function EventCard({ evt, selected, onToggle, disabled }: {
  evt: EventCardRender;
  selected: boolean;
  onToggle: () => void;
  disabled: boolean;
}) {
  const dtKey = Math.min(5, Math.max(-2, evt.dramaticTension));
  const dtColor = DT_COLORS[dtKey] ?? '#888';
  return (
    <button
      className={`${styles.optionCard} ${selected ? styles.selected : ''}`}
      onClick={onToggle}
      disabled={disabled}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
        <span style={{ color: 'var(--accent)' }}>{evt.name}</span>
        <span style={{ color: dtColor, fontSize: '0.75rem', fontWeight: 'bold' }}>
          {evt.dramaticTension > 0 ? '+' : ''}{evt.dramaticTension} DT
        </span>
      </div>
      <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
        {evt.effectSummary}
        {!evt.tearingAllowed && (
          <span style={{ color: 'var(--orange)', marginLeft: '0.5rem' }}>[no tear]</span>
        )}
      </div>
    </button>
  );
}
