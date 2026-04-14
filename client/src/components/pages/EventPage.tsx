import type { EventRight, LeftPageRender, PlayerAction } from '../../api/types';
import {
  EventOptionSelectAction, EventConfirmAction, EventContinueAction,
  DismissCrewMemberAction, ShopBuyAction, ShopExitAction, TearAction
} from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: EventRight;
  left: LeftPageRender;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function EventPage({ page, left, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>{page.eventName}</div>
        <div className={styles.committedBadge}>✓ Complete</div>
        <button className={styles.primaryBtn} onClick={() => dispatch(EventContinueAction())} disabled={disabled}>
          Continue →
        </button>
      </div>
    );
  }

  // Shop event
  if (page.shopData) {
    const shop = page.shopData;
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>{page.eventName}</div>
        <div className={styles.narrative}>{page.narrativeText}</div>
        <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', marginTop: '-0.25rem' }}>
          Coin: <span style={{ color: 'var(--accent)' }}>⬡ {left.coin}</span>
        </div>
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Wares</div>
          <div className={styles.optionGrid}>
            {shop.items.map(item => (
              <div key={item.itemId} style={{ display: 'flex', gap: '0.5rem', alignItems: 'flex-start' }}>
                <button
                  className={styles.optionCard}
                  onClick={() => dispatch(ShopBuyAction(item.itemId, item.cost))}
                  disabled={disabled || !item.canAfford}
                  style={{ flex: 1 }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span style={{ color: 'var(--accent)' }}>{item.name}</span>
                    <span style={{ color: item.canAfford ? 'var(--green)' : 'var(--red)', fontWeight: 'bold' }}>
                      ⬡ {item.cost}
                    </span>
                  </div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.15rem' }}>
                    {item.description}
                  </div>
                </button>
              </div>
            ))}
          </div>
        </div>
        <div className={styles.actionRow}>
          <button className={styles.secondaryBtn} onClick={() => dispatch(ShopExitAction())} disabled={disabled}>
            Leave Shop
          </button>
        </div>
      </div>
    );
  }

  // Option event
  if (page.optionLabels && !page.selectedOption) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>{page.eventName}</div>
        <div className={styles.narrative}>{page.narrativeText}</div>
        <div className={styles.effectBox}>{page.effectDescription}</div>
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Choose</div>
          <div className={styles.optionGrid}>
            {page.optionLabels.map((label, i) => (
              <button
                key={i}
                className={`${styles.optionCard} ${page.selectedOption === i ? styles.selected : ''}`}
                onClick={() => dispatch(EventOptionSelectAction(i))}
                disabled={disabled}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Crew dismissal
  if (page.needsCrewDismissal) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>{page.eventName}</div>
        <div className={styles.narrative}>{page.narrativeText}</div>
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Choose a crew member to dismiss</div>
          <div className={styles.optionGrid}>
            {left.crew.filter(c => !c.isDead).map(c => (
              <button
                key={c.id}
                className={styles.optionCard}
                onClick={() => dispatch(DismissCrewMemberAction(c.id))}
                disabled={disabled}
              >
                <span style={{ color: 'var(--accent)' }}>{c.name}</span>
                <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                  {c.raceName} · {c.abilities[0]?.typeName}
                </span>
              </button>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Option event with a selection made — show confirm button
  if (page.optionLabels && page.selectedOption !== null) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>{page.eventName}</div>
        <div className={styles.narrative}>{page.narrativeText}</div>
        <div className={styles.effectBox}>{page.effectDescription}</div>
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Selected: {page.optionLabels[page.selectedOption!]}</div>
        </div>
        <div className={styles.actionRow}>
          <button className={styles.primaryBtn} onClick={() => dispatch(EventConfirmAction())} disabled={disabled}>
            Confirm Choice
          </button>
        </div>
      </div>
    );
  }

  // Announcement — no choice required
  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>{page.eventName}</div>
      <div className={styles.narrative}>{page.narrativeText}</div>
      <div className={styles.effectBox}>{page.effectDescription}</div>
      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(EventContinueAction())}
          disabled={disabled}
        >
          Continue →
        </button>
        {page.tearingAllowed_ && (
          <button className={styles.secondaryBtn} onClick={() => dispatch(TearAction())} disabled={disabled}>
            Tear Page
          </button>
        )}
      </div>
    </div>
  );
}
