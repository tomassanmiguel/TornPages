import type { PrologueCrewRight, PlayerAction } from '../../api/types';
import { PrologueCrewAttrAction, ConfirmPrologueCrewAction } from '../../api/types';
import styles from './shared.module.css';

interface Props {
  page: PrologueCrewRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

const RACE_NAMES = ['Human', 'Verdant', 'Mechanis', 'Replicant', 'Luminae', 'Voidborn', 'Saurid'];
const ABILITY_COLORS: Record<string, string> = {
  Shield: '#4090d0', Hack: '#40d090', Charge: '#d09040',
  Boost: '#d0d040', Barrier: '#9040d0', Heal: '#40c060', Fusion: '#d04090'
};

export function PrologueCrewPage({ page, dispatch, disabled }: Props) {
  const opts = page.options;
  const sel = page.selections;

  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Crew Member {page.crewIndex + 1}</div>
        <div className={styles.committedBadge}>✓ Committed</div>
      </div>
    );
  }

  const allSelected = sel &&
    sel.nameIndex >= 0 && sel.raceIndex >= 0 && sel.backstoryIndex >= 0 &&
    sel.abilityIndex >= 0 && sel.statIndex >= 0 && sel.traitIndex >= 0;

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>
        Choose Crew Member {page.crewIndex + 1}
        <span style={{ color: ABILITY_COLORS[page.targetAbility] ?? '#888', fontSize: '0.85rem', marginLeft: '0.5rem' }}>
          ({page.targetAbility})
        </span>
      </div>
      <p className={styles.pageSubtitle}>Select one option for each attribute.</p>

      <AttrSection title="Name" count={opts.nameOptions.length}>
        {opts.nameOptions.map((n, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.nameIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('name', i))}
            disabled={disabled}
          >
            {n}
          </button>
        ))}
      </AttrSection>

      <AttrSection title="Race" count={opts.raceOptions.length}>
        {opts.raceOptions.map((r, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.raceIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('race', i))}
            disabled={disabled}
          >
            {RACE_NAMES[r] ?? `Race ${r}`}
          </button>
        ))}
      </AttrSection>

      <AttrSection title="Backstory" count={opts.backstoryOptions.length}>
        {opts.backstoryOptions.map((b, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.backstoryIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('backstory', i))}
            disabled={disabled}
          >
            <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>{b}</span>
          </button>
        ))}
      </AttrSection>

      <AttrSection title="Stats" count={opts.statOptions.length}>
        {opts.statOptions.map((s, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.statIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('stats', i))}
            disabled={disabled}
          >
            <span style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
              <span>Res {s.resolve}</span>
              <span>Int {s.intelligence}</span>
              <span>Cha {s.charisma}</span>
              <span>End {s.endurance}</span>
            </span>
          </button>
        ))}
      </AttrSection>

      <AttrSection title="Ability" count={opts.abilityOptions.length}>
        {opts.abilityOptions.map((a, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.abilityIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('ability', i))}
            disabled={disabled}
          >
            <span style={{ color: ABILITY_COLORS[a.type] ?? '#888' }}>{a.type}</span>
            {a.rarity === 1 && (
              <span style={{ color: 'var(--orange)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>[Rare]</span>
            )}
          </button>
        ))}
      </AttrSection>

      <AttrSection title="Trait" count={opts.traitOptions.length}>
        {opts.traitOptions.map((t, i) => (
          <button
            key={i}
            className={`${styles.optionCard} ${sel?.traitIndex === i ? styles.selected : ''}`}
            onClick={() => dispatch(PrologueCrewAttrAction('trait', i))}
            disabled={disabled}
          >
            {t ? (
              <>
                <span style={{ color: t.isNegative ? 'var(--red)' : 'var(--green)' }}>{t.name}</span>
                <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                  {t.description.substring(0, 60)}
                </span>
              </>
            ) : (
              <span style={{ color: 'var(--text-muted)' }}>No trait</span>
            )}
          </button>
        ))}
      </AttrSection>

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ConfirmPrologueCrewAction())}
          disabled={disabled || !allSelected}
        >
          Confirm Crew Member
        </button>
      </div>
    </div>
  );
}

function AttrSection({ title, count, children }: { title: string; count: number; children: React.ReactNode }) {
  void count;
  return (
    <div className={styles.section}>
      <div className={styles.sectionTitle}>{title}</div>
      <div className={styles.optionGrid}>{children}</div>
    </div>
  );
}
