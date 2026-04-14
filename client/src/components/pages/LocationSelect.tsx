import type { LocationSelectRight, PlayerAction } from '../../api/types';
import { SelectLocationAction, ConfirmLocationAction } from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: LocationSelectRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function LocationSelectPage({ page, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Location</div>
        <div className={styles.committedBadge}>✓ Location set</div>
      </div>
    );
  }

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>Choose a Location</div>
      <p className={styles.pageSubtitle}>Where will the ship travel next?</p>

      <div className={styles.optionGrid}>
        {page.options.map(loc => (
          <button
            key={loc.id}
            className={`${styles.optionCard} ${page.selectedLocationId === loc.id ? styles.selected : ''}`}
            onClick={() => dispatch(SelectLocationAction(loc.id))}
            disabled={disabled}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline' }}>
              <span style={{ color: 'var(--accent)', fontWeight: 'bold' }}>{loc.name}</span>
              {loc.specialEventName && (
                <span style={{ color: 'var(--purple)', fontSize: '0.72rem' }}>
                  ✦ {loc.specialEventName}
                </span>
              )}
            </div>
            <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', marginTop: '0.25rem', fontStyle: 'italic' }}>
              {loc.loreDescription}
            </div>
          </button>
        ))}
      </div>

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ConfirmLocationAction())}
          disabled={disabled || !page.selectedLocationId}
        >
          Set Course
        </button>
      </div>
    </div>
  );
}
