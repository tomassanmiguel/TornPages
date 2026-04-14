import type { PrologueBoonRight, PlayerAction } from '../../api/types';
import { BoonSelectAction, ConfirmBoonAction } from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: PrologueBoonRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function PrologueBoonPage({ page, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Starting Boon</div>
        <div className={styles.committedBadge}>✓ Boon selected</div>
      </div>
    );
  }

  const hasSelection = page.selectedModId || page.selectedFormationId || page.selectedDeployableId;

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>Choose Your Starting Boon</div>
      <p className={styles.pageSubtitle}>Select one gift to begin the journey.</p>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Mods</div>
        <div className={styles.optionGrid}>
          {page.modOptions.map(m => (
            <button
              key={m.modId}
              className={`${styles.optionCard} ${page.selectedModId === m.modId ? styles.selected : ''}`}
              onClick={() => dispatch(BoonSelectAction(m.modId, null, null))}
              disabled={disabled}
            >
              <span style={{ color: 'var(--accent)' }}>{m.name}</span>
              <span style={{ color: m.isGlitch ? 'var(--red)' : 'var(--text-muted)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                [{['S','M','L'][m.size]}] {m.isGlitch ? '⚠ Glitch' : ''}
              </span>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>{m.description}</div>
            </button>
          ))}
        </div>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Formations</div>
        <div className={styles.optionGrid}>
          {page.formationOptions.map(f => (
            <button
              key={f.id}
              className={`${styles.optionCard} ${page.selectedFormationId === f.id ? styles.selected : ''}`}
              onClick={() => dispatch(BoonSelectAction(null, f.id, null))}
              disabled={disabled}
            >
              <span style={{ color: 'var(--accent)' }}>{f.name}</span>
              <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                {f.slots.length} slots
              </span>
              {f.passiveDescription && (
                <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                  {f.passiveDescription}
                </div>
              )}
            </button>
          ))}
        </div>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Deployables</div>
        <div className={styles.optionGrid}>
          {page.deployableOptions.map(d => (
            <button
              key={d.deployableId}
              className={`${styles.optionCard} ${page.selectedDeployableId === d.deployableId ? styles.selected : ''}`}
              onClick={() => dispatch(BoonSelectAction(null, null, d.deployableId))}
              disabled={disabled}
            >
              <span style={{ color: 'var(--accent2)' }}>{d.name}</span>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>{d.description}</div>
            </button>
          ))}
        </div>
      </div>

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ConfirmBoonAction())}
          disabled={disabled || !hasSelection}
        >
          Confirm Boon
        </button>
      </div>
    </div>
  );
}
