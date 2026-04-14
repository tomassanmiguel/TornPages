import type { LeftPageRender, RightPageRender, CrewRender, FormationRender, PlayerAction } from '../api/types';
import styles from './LeftPage.module.css';

interface Props {
  left: LeftPageRender;
  right: RightPageRender;
  dispatch: (action: PlayerAction) => void;
  isLoading: boolean;
}

const RARITY_COLORS = ['#888', '#70c870', '#7090d0', '#d0a040', '#c070c0'];
const RARITY_NAMES = ['Starting', 'Common', 'Uncommon', 'Rare', 'Unique'];
const SLOT_COLORS: Record<string, string> = {
  Empty: '#6080b0',
  Boosted: '#c0b040',
  Pain: '#c06060',
  Heal: '#60c060',
  Growth: '#8060c0',
  Fatigue: '#a06060',
  MinusFatigue: '#60a060',
  Kill: '#c04040',
};

export function LeftPage({ left }: Props) {
  return (
    <div className={styles.leftPage}>
      {/* Ship Status */}
      <div className={styles.shipStatus}>
        <div className={styles.statusRow}>
          <StatusBar label="Hull" current={left.hullCurrent} max={left.hullMax} color="#d06060" />
          <StatusBar label="Morale" current={left.morale} max={100} color="#60a0c0" />
        </div>
        <div className={styles.statusRow2}>
          <Stat label="Coin" value={`⬡ ${left.coin}`} />
          <Stat label="Pages" value={`${left.pagesRemaining}`} />
          <Stat label="Chapter" value={`${left.chapterNumber}`} />
          <Stat label="Act" value={`${left.actNumber}`} />
        </div>
      </div>

      {/* Crew */}
      {left.crew.length > 0 && (
        <Section title="Crew">
          <div className={styles.crewGrid}>
            {left.crew.map(c => <CrewCard key={c.id} crew={c} />)}
          </div>
        </Section>
      )}

      {/* Formations */}
      {left.formations.length > 0 && (
        <Section title="Formations">
          <div className={styles.formationList}>
            {left.formations.map(f => <FormationCard key={f.id} formation={f} isLastUsed={f.id === left.lastUsedFormationId} />)}
          </div>
        </Section>
      )}

      {/* Systems */}
      <Section title="Ship Systems">
        <div className={styles.systemsGrid}>
          <SystemCard sys={left.systems.engine} />
          <SystemCard sys={left.systems.cabin} />
          <SystemCard sys={left.systems.hull} />
          <SystemCard sys={left.systems.computers} />
        </div>
      </Section>

      {/* Deployables */}
      {left.deployables.length > 0 && (
        <Section title="Deployables">
          <div className={styles.deployableList}>
            {left.deployables.map(d => (
              <div key={d.instanceId} className={styles.deployableItem} title={d.description}>
                {d.name}
              </div>
            ))}
          </div>
        </Section>
      )}

      {/* Mod storage */}
      {left.modStorage.length > 0 && (
        <Section title="Mod Storage">
          <div className={styles.modList}>
            {left.modStorage.map(m => (
              <div key={m.instanceId} className={`${styles.modItem} ${m.isGlitch ? styles.glitch : ''}`} title={m.description}>
                <span className={styles.modName}>{m.name}</span>
                <span className={styles.modSize}>{'SMl'[m.size]}</span>
              </div>
            ))}
          </div>
        </Section>
      )}
    </div>
  );
}

function StatusBar({ label, current, max, color }: { label: string; current: number; max: number; color: string }) {
  const pct = Math.max(0, Math.min(100, (current / max) * 100));
  return (
    <div className={styles.statusBar}>
      <div className={styles.statusBarLabel}>{label} {current}/{max}</div>
      <div className={styles.statusBarBg}>
        <div className={styles.statusBarFill} style={{ width: `${pct}%`, background: color }} />
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.statItem}>
      <span className={styles.statLabel}>{label}</span>
      <span className={styles.statValue}>{value}</span>
    </div>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className={styles.section}>
      <div className={styles.sectionTitle}>{title}</div>
      {children}
    </div>
  );
}

function CrewCard({ crew }: { crew: CrewRender }) {
  const ability = crew.abilities[0];
  const fatigueUsed = crew.fatigueBaseline;
  const isExhausted = crew.isExhausted;

  return (
    <div className={`${styles.crewCard} ${crew.isDead ? styles.dead : ''} ${crew.isPanicked ? styles.panicked : ''}`}>
      <div className={styles.crewHeader}>
        <span className={styles.crewName}>{crew.name}</span>
        <span className={styles.crewRace}>{crew.raceName}</span>
      </div>
      <div className={styles.crewStats}>
        <span title="Resolve">Res {crew.stats.resolve}</span>
        <span title="Intelligence">Int {crew.stats.intelligence}</span>
        <span title="Charisma">Cha {crew.stats.charisma}</span>
        <span title="Endurance">End {crew.stats.endurance}</span>
      </div>
      <div className={styles.crewBottom}>
        <span className={styles.crewHp}>
          HP {crew.hpCurrent}/{crew.hpMax}
        </span>
        {ability && (
          <span className={`${styles.crewAbility} ${styles[`ability${ability.typeName}`]}`}>
            {ability.isDual ? '×½ ' : ''}{ability.typeName}
          </span>
        )}
        <span className={`${styles.crewEl} ${isExhausted ? styles.exhausted : ''}`} title="Effect Level">
          EL {crew.effectiveEffectLevel}
        </span>
        {fatigueUsed > 0 && (
          <span className={styles.crewFatigue}>
            Fat {fatigueUsed}/{crew.stats.endurance}
          </span>
        )}
      </div>
      {crew.traits.length > 0 && (
        <div className={styles.crewTraits}>
          {crew.traits.map(t => (
            <span key={t.id} className={`${styles.traitPill} ${t.isNegative ? styles.traitNeg : styles.traitPos}`} title={t.description}>
              {t.name}
            </span>
          ))}
        </div>
      )}
      {crew.isDead && <div className={styles.deadBanner}>DEAD</div>}
      {crew.isPanicked && <div className={styles.panicBanner}>PANICKED</div>}
    </div>
  );
}

function FormationCard({ formation, isLastUsed }: { formation: FormationRender; isLastUsed: boolean }) {
  return (
    <div className={`${styles.formationCard} ${isLastUsed ? styles.lastUsed : ''}`}>
      <div className={styles.formationHeader}>
        <span className={styles.formationName}>{formation.name}</span>
        <span className={styles.formationRarity} style={{ color: RARITY_COLORS[formation.rarity] }}>
          {RARITY_NAMES[formation.rarity]}
        </span>
      </div>
      <div className={styles.formationSlots}>
        {formation.slots.map((s, i) => (
          <div
            key={i}
            className={styles.slotDot}
            style={{ background: SLOT_COLORS[s.typeName] ?? '#666' }}
            title={`${s.typeName} [${s.connectedLanes.join(',')}]${s.isDouble ? ' ×2' : ''}${s.slottedCrewId ? ' (crew)' : ''}`}
          >
            {s.slottedCrewId ? '●' : s.isDouble ? '2' : ''}
          </div>
        ))}
      </div>
      {formation.passiveDescription && (
        <div className={styles.formationPassive}>{formation.passiveDescription}</div>
      )}
    </div>
  );
}

function SystemCard({ sys }: { sys: { name: string; level: number; modSlots: number; slotsUsed: number; primaryValueLabel: string; primaryValue: number; installedMods: { instanceId: string; name: string; description: string; isGlitch: boolean; size: number }[] } }) {
  return (
    <div className={styles.systemCard}>
      <div className={styles.systemHeader}>
        <span className={styles.systemName}>{sys.name}</span>
        <span className={styles.systemLevel}>Lv {sys.level}</span>
      </div>
      <div className={styles.systemValue}>
        {sys.primaryValueLabel}: {sys.primaryValue}
      </div>
      {sys.installedMods.length > 0 && (
        <div className={styles.systemMods}>
          {sys.installedMods.map(m => (
            <span key={m.instanceId} className={`${styles.modDot} ${m.isGlitch ? styles.glitchDot : ''}`} title={`${m.name}: ${m.description}`}>
              {m.name.substring(0, 10)}
            </span>
          ))}
        </div>
      )}
      <div className={styles.systemSlots}>
        Slots: {sys.slotsUsed}/{sys.modSlots}
      </div>
    </div>
  );
}
