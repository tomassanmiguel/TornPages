import { useState } from 'react';
import type { CombatRight, LeftPageRender, PlayerAction, FormationRender, LaneRender, SlotCombatRender } from '../../api/types';
import {
  SelectFormationAction, SlotCrewAction, UnslotCrewAction,
  ReadyUpAction, UseDeployableAction, TearAction
} from '../../api/types';
import styles from './shared.module.css';
import cStyles from './CombatPage.module.css';

interface Props {
  page: CombatRight;
  left: LeftPageRender;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

const SLOT_COLORS: Record<string, string> = {
  Empty: '#4060b0', Boosted: '#b0a030', Pain: '#b04040', Heal: '#40b040',
  Growth: '#7050b0', Fatigue: '#904040', MinusFatigue: '#408040', Kill: '#c03030',
};

export function CombatPage({ page, left, dispatch, disabled }: Props) {
  const [selectedCrewId, setSelectedCrewId] = useState<string | null>(null);

  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Combat — Page Complete</div>
        <div className={styles.committedBadge}>✓ Page resolved</div>
      </div>
    );
  }

  const aliveCrew = left.crew.filter(c =>
    !c.isDead &&
    c.id !== page.kidnappedCrewId &&
    !page.panickedCrewIds.includes(c.id)
  );

  // Crew slotted anywhere
  const slottedIds = new Set(
    page.lanes.flatMap(l => l.slots.map(s => s.slottedCrewId).filter(Boolean))
  );

  const crewForSlotting = aliveCrew.filter(c => !slottedIds.has(c.id));

  return (
    <div className={styles.page}>
      {/* Header */}
      <div className={cStyles.combatHeader}>
        <div className={cStyles.enemyName}>
          {page.enemy.isBoss && <span style={{ color: 'var(--red)' }}>⚔ Boss · </span>}
          {page.enemy.name}
          {page.enemy.superShieldCurrent != null && (
            <span style={{ color: 'var(--orange)', marginLeft: '0.5rem' }}>
              [Shield: {page.enemy.superShieldCurrent}]
            </span>
          )}
        </div>
        <div className={cStyles.chargeBar}>
          <span className={cStyles.chargeLabel}>Charge</span>
          <span className={cStyles.chargeValue}>{page.chargeAccumulated}</span>
          <span style={{ color: 'var(--text-muted)' }}>/{page.chargeThreshold}</span>
        </div>
      </div>

      {/* Special states */}
      {page.isPhantomPage && (
        <div className={cStyles.stateBanner} style={{ color: 'var(--purple)' }}>
          ◆ Phantom page — no threat this page
        </div>
      )}
      {page.isInvulnerablePage && (
        <div className={cStyles.stateBanner} style={{ color: 'var(--green)' }}>
          ◆ Invulnerable — no hull damage this page
        </div>
      )}

      {/* Enemy intents */}
      {!page.enemy.intentsHidden && page.enemy.intents.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Enemy Intents</div>
          <div className={cStyles.intentList}>
            {page.enemy.intents.map((intent, i) => (
              <div key={i} className={cStyles.intent}>
                <span className={cStyles.intentLane}>[{intent.laneName}]</span>
                <span className={cStyles.intentWeapon}>{intent.weaponName}</span>
                <span className={cStyles.intentEffect}>{intent.effectDescription}</span>
                <span className={cStyles.intentValue} style={{ color: 'var(--red)' }}>
                  {intent.resolvedValue > 0 ? `→ ${intent.resolvedValue}` : ''}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
      {page.enemy.intentsHidden && (
        <div className={cStyles.stateBanner} style={{ color: 'var(--text-muted)' }}>
          Enemy intents are hidden
        </div>
      )}

      {/* Formation selection */}
      <div className={styles.section}>
        <div className={styles.sectionTitle}>Formation</div>
        <div className={cStyles.formationRow}>
          {page.availableFormations.map(f => (
            <FormationOption
              key={f.id}
              f={f}
              isActive={f.id === page.activeFormationId}
              onClick={() => dispatch(SelectFormationAction(f.id))}
              disabled={disabled}
            />
          ))}
        </div>
      </div>

      {/* Lanes */}
      {page.activeFormationId && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>
            Formation Slots
            {selectedCrewId && (
              <span style={{ color: 'var(--accent)', marginLeft: '0.5rem' }}>
                — click a slot to place {left.crew.find(c => c.id === selectedCrewId)?.name}
              </span>
            )}
          </div>
          <div className={cStyles.lanesGrid}>
            {page.lanes.map(lane => (
              <LaneColumn
                key={lane.laneIndex}
                lane={lane}
                selectedCrewId={selectedCrewId}
                onSlot={(slotIdx) => {
                  if (selectedCrewId) {
                    dispatch(SlotCrewAction(selectedCrewId, slotIdx));
                    setSelectedCrewId(null);
                  }
                }}
                onUnslot={(slotIdx) => dispatch(UnslotCrewAction(slotIdx))}
                disabled={disabled}
              />
            ))}
          </div>
        </div>
      )}

      {/* Crew for slotting */}
      {crewForSlotting.length > 0 && page.activeFormationId && !page.awaitingReadyUp && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Unslotted Crew — click to select for placement</div>
          <div className={cStyles.crewRow}>
            {crewForSlotting.map(c => (
              <button
                key={c.id}
                className={`${cStyles.crewChip} ${selectedCrewId === c.id ? cStyles.crewSelected : ''}`}
                onClick={() => setSelectedCrewId(selectedCrewId === c.id ? null : c.id)}
                disabled={disabled}
              >
                {c.name}
                <span style={{ color: 'var(--text-muted)', marginLeft: '0.4rem', fontSize: '0.72rem' }}>
                  EL{c.effectiveEffectLevel} {c.abilities[0]?.typeName}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Forecast */}
      {page.forecast && (
        <div className={cStyles.forecast}>
          <div className={cStyles.forecastTitle}>Forecast</div>
          <div className={cStyles.forecastRow}>
            {['Left','Center','Right'].map((name, i) => (
              <div key={name} className={cStyles.forecastLane}>
                <div className={cStyles.forecastLaneName}>{name}</div>
                <div style={{ color: 'var(--accent2)' }}>Shld {page.forecast!.forecastShield[i]}</div>
                <div style={{ color: 'var(--red)' }}>Thrt {page.forecast!.forecastThreat[i]}</div>
                <div style={{ color: 'var(--green)' }}>Hack {page.forecast!.forecastHack[i]}</div>
              </div>
            ))}
            <div className={cStyles.forecastLane}>
              <div className={cStyles.forecastLaneName}>Total</div>
              <div style={{ color: 'var(--orange)' }}>+{page.forecast.forecastChargeGain} charge</div>
              {page.forecast.forecastHullDamage > 0 && (
                <div style={{ color: 'var(--red)' }}>-{page.forecast.forecastHullDamage} hull</div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Active modifiers */}
      {page.activeModifiers.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Combat Modifiers</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.3rem' }}>
            {page.activeModifiers.map((m, i) => (
              <span
                key={i}
                className={styles.tag}
                style={{ color: m.isPositive ? 'var(--green)' : 'var(--red)' }}
              >
                {m.name}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Deployables */}
      {page.availableDeployables.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Deployables</div>
          <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap' }}>
            {page.availableDeployables.map(d => (
              <button
                key={d.instanceId}
                className={styles.secondaryBtn}
                onClick={() => dispatch(UseDeployableAction(d.instanceId))}
                disabled={disabled}
                title={d.description}
              >
                {d.name}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Panicked / kidnapped notices */}
      {(page.panickedCrewIds.length > 0 || page.kidnappedCrewId) && (
        <div className={cStyles.notices}>
          {page.kidnappedCrewId && (
            <span style={{ color: 'var(--red)' }}>
              ✗ {left.crew.find(c => c.id === page.kidnappedCrewId)?.name ?? 'Crew'} is kidnapped
            </span>
          )}
          {page.panickedCrewIds.map(id => (
            <span key={id} style={{ color: 'var(--orange)' }}>
              ⚡ {left.crew.find(c => c.id === id)?.name ?? id} is panicking
            </span>
          ))}
        </div>
      )}

      {/* Actions */}
      <div className={styles.actionRow}>
        {page.awaitingReadyUp ? (
          <button className={styles.primaryBtn} onClick={() => dispatch(ReadyUpAction())} disabled={disabled}>
            Resolve Page →
          </button>
        ) : (
          <button
            className={styles.primaryBtn}
            onClick={() => dispatch(ReadyUpAction())}
            disabled={disabled || !page.activeFormationId}
          >
            Ready Up
          </button>
        )}
        <button className={styles.secondaryBtn} onClick={() => dispatch(TearAction())} disabled={disabled}>
          Tear Page
        </button>
      </div>
    </div>
  );
}

function FormationOption({ f, isActive, onClick, disabled }: {
  f: FormationRender; isActive: boolean; onClick: () => void; disabled: boolean;
}) {
  return (
    <button
      className={`${cStyles.formationOption} ${isActive ? cStyles.formationActive : ''}`}
      onClick={onClick}
      disabled={disabled}
    >
      <div className={cStyles.formationOptName}>{f.name}</div>
      <div className={cStyles.formationSlots}>
        {f.slots.map((s, i) => (
          <div
            key={i}
            className={cStyles.slotDot}
            style={{
              background: SLOT_COLORS[s.typeName] ?? '#666',
              opacity: s.slottedCrewId ? 1 : 0.5,
              border: s.slottedCrewId ? '1px solid #fff4' : 'none',
            }}
            title={`${s.typeName} [${s.connectedLanes.join(',')}]${s.slottedCrewId ? ' ●' : ''}`}
          />
        ))}
      </div>
    </button>
  );
}

function LaneColumn({ lane, selectedCrewId, onSlot, onUnslot, disabled }: {
  lane: LaneRender;
  selectedCrewId: string | null;
  onSlot: (slotIdx: number) => void;
  onUnslot: (slotIdx: number) => void;
  disabled: boolean;
}) {
  return (
    <div className={cStyles.laneCol}>
      <div className={cStyles.laneName}>{lane.name}</div>
      {/* Lane stats */}
      <div className={cStyles.laneStats}>
        {lane.lockOn > 0 && <span style={{ color: 'var(--orange)' }}>LockOn {lane.lockOn}</span>}
        {lane.burnStacks > 0 && <span style={{ color: 'var(--red)' }}>Burn {lane.burnStacks}</span>}
        {lane.barrierStacks > 0 && <span style={{ color: 'var(--accent2)' }}>Barrier {lane.barrierStacks}</span>}
        {lane.fusionStacks > 0 && <span style={{ color: 'var(--purple)' }}>Fusion {lane.fusionStacks}</span>}
        {lane.threat > 0 && <span style={{ color: 'var(--red)', fontWeight: 'bold' }}>⚠ Threat {lane.threat}</span>}
      </div>
      {lane.slots.map(slot => (
        <SlotCell
          key={slot.slotIndex}
          slot={slot}
          canPlace={selectedCrewId != null && !slot.slottedCrewId}
          onSlot={() => onSlot(slot.slotIndex)}
          onUnslot={() => onUnslot(slot.slotIndex)}
          disabled={disabled}
        />
      ))}
    </div>
  );
}

function SlotCell({ slot, canPlace, onSlot, onUnslot, disabled }: {
  slot: SlotCombatRender;
  canPlace: boolean;
  onSlot: () => void;
  onUnslot: (slotIdx: number) => void;
  disabled: boolean;
}) {
  const slotColor = SLOT_COLORS[slot.typeName] ?? '#666';

  return (
    <div
      className={`${cStyles.slotCell} ${canPlace && !disabled ? cStyles.slotPlaceable : ''} ${slot.slottedCrewId ? cStyles.slotFilled : ''}`}
      style={{ borderColor: slotColor }}
      onClick={() => {
        if (disabled) return;
        if (canPlace) onSlot();
        else if (slot.slottedCrewId) onUnslot(slot.slotIndex);
      }}
    >
      <div className={cStyles.slotType} style={{ color: slotColor }}>{slot.typeName}</div>
      {slot.slottedCrewId ? (
        <div className={cStyles.slottedCrew}>
          <span>{slot.slottedCrewName}</span>
          {slot.forecastText && (
            <span className={cStyles.forecast_text}>{slot.forecastText}</span>
          )}
        </div>
      ) : (
        <div className={cStyles.emptySlot}>{canPlace ? 'Place here' : 'Empty'}</div>
      )}
      {slot.isDouble && <span className={cStyles.doubleBadge}>×2</span>}
    </div>
  );
}
