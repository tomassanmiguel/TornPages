import { useEffect, useState } from 'react';
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
  const [expandedTrait, setExpandedTrait] = useState<number | null>(null);

  // Ability is always the forced type — auto-select index 0 silently
  useEffect(() => {
    if (!page.isCommitted_ && (sel === null || sel.abilityIndex < 0)) {
      dispatch(PrologueCrewAttrAction('ability', 0));
    }
  }, [page.crewIndex]); // re-run for each new crew page

  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Crew Member {page.crewIndex + 1}</div>
        <div className={styles.committedBadge}>✓ Committed</div>
      </div>
    );
  }

  const allSelected = sel !== null &&
    sel.nameIndex >= 0 && sel.raceIndex >= 0 && sel.backstoryIndex >= 0 &&
    sel.statIndex >= 0 && sel.traitIndex >= 0;
  // abilityIndex will be set by useEffect — don't gate on it

  const abilityColor = ABILITY_COLORS[page.targetAbility] ?? '#888';

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>
        Crew Member {page.crewIndex + 1}
        <span style={{ color: abilityColor, fontSize: '0.85rem', marginLeft: '0.5rem' }}>
          [{page.targetAbility}]
        </span>
      </div>
      <p className={styles.pageSubtitle}>Select one option for each attribute.</p>

      {/* Name */}
      <AttrSection title="Name">
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

      {/* Race */}
      <AttrSection title="Race">
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

      {/* Backstory */}
      <AttrSection title="Backstory">
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

      {/* Stats */}
      <AttrSection title="Stats">
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

      {/* Trait */}
      <AttrSection title="Trait">
        {opts.traitOptions.map((t, i) => (
          <div key={i}>
            <button
              className={`${styles.optionCard} ${sel?.traitIndex === i ? styles.selected : ''}`}
              onClick={() => dispatch(PrologueCrewAttrAction('trait', i))}
              disabled={disabled}
              style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
            >
              <span>
                {t ? (
                  <span style={{ color: t.isNegative ? 'var(--red)' : 'var(--green)' }}>{t.name}</span>
                ) : (
                  <span style={{ color: 'var(--text-muted)' }}>No trait</span>
                )}
              </span>
              {t && (
                <button
                  className={styles.infoBtn}
                  onClick={e => { e.stopPropagation(); setExpandedTrait(expandedTrait === i ? null : i); }}
                >i</button>
              )}
            </button>
            {t && expandedTrait === i && (
              <div className={styles.infoExpand}>{t.description}</div>
            )}
          </div>
        ))}
      </AttrSection>

      {/* Ability — auto-selected, shown as info only */}
      <div className={styles.section}>
        <div className={styles.sectionTitle}>Ability</div>
        <div className={styles.infoRow}>
          <span style={{ color: abilityColor }}>{page.targetAbility}</span>
          <span style={{ color: 'var(--text-muted)', fontSize: '0.78rem' }}>auto-assigned</span>
        </div>
      </div>

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

function AttrSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className={styles.section}>
      <div className={styles.sectionTitle}>{title}</div>
      <div className={styles.optionGrid}>{children}</div>
    </div>
  );
}
