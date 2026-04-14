import type { CombatRewardRight, PlayerAction } from '../../api/types';
import { SelectRewardAction, ConfirmRewardAction } from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: CombatRewardRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function CombatRewardPage({ page, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Combat Victory!</div>
        <div className={styles.committedBadge}>✓ Reward collected</div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>Victory!</div>
      <div className={styles.effectBox}>
        Coin reward: <span style={{ color: 'var(--accent)' }}>+⬡ {page.coinReward}</span>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Choose a Formation Reward</div>
        <div className={styles.optionGrid}>
          {page.formationOptions.map(f => (
            <button
              key={f.id}
              className={`${styles.optionCard} ${page.selectedFormationId === f.id ? styles.selected : ''}`}
              onClick={() => dispatch(SelectRewardAction(f.id))}
              disabled={disabled}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
                <span style={{ color: 'var(--accent)' }}>{f.name}</span>
                <span style={{ color: 'var(--text-muted)', fontSize: '0.72rem' }}>
                  {['Starting','Common','Uncommon','Rare','Unique'][f.rarity]} · {f.slots.length} slots
                </span>
              </div>
              {f.passiveDescription && (
                <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                  {f.passiveDescription}
                </div>
              )}
            </button>
          ))}
        </div>
      </div>

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ConfirmRewardAction())}
          disabled={disabled || !page.selectedFormationId}
        >
          Collect Reward
        </button>
      </div>
    </div>
  );
}
