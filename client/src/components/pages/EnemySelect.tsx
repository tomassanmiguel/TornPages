import type { EnemySelectRight, PlayerAction, WeaponRender } from '../../api/types';
import { SelectWeaponAction, SelectEnemyAbilityAction, ConfirmEnemyAction } from '../../api/types';
import styles from './shared.module.css';
import eStyles from './EnemySelect.module.css';

interface Props {
  page: EnemySelectRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

const EFFECT_TYPE_NAMES = ['None', 'Threat', 'Piercing Threat', 'Burn', 'Lock-On+', 'Lock-On−', 'Charge−', 'Morale−', 'Counter Threat', 'Kill Crew'];

function WeaponCard({ weapon, isSelected, onClick, disabled, locked }: {
  weapon: WeaponRender;
  isSelected?: boolean;
  onClick?: () => void;
  disabled?: boolean;
  locked?: boolean;
}) {
  const eff = (e: { type: number; intValue: number }) =>
    e.type === 0 ? '—' : `${EFFECT_TYPE_NAMES[e.type] ?? e.type} ${e.intValue}`;

  return (
    <div
      className={`${eStyles.weaponCard} ${isSelected ? eStyles.selected : ''} ${locked ? eStyles.locked : ''}`}
      onClick={!locked && !disabled ? onClick : undefined}
      style={{ cursor: locked || disabled ? 'default' : 'pointer' }}
    >
      <div className={eStyles.weaponName}>
        {weapon.name}
        {locked && <span style={{ color: 'var(--red)', fontSize: '0.7rem', marginLeft: '0.4rem' }}>[locked]</span>}
        {weapon.randomiseLanes && <span style={{ color: 'var(--orange)', fontSize: '0.7rem', marginLeft: '0.4rem' }}>[random lanes]</span>}
      </div>
      <div className={eStyles.weaponEffects}>
        <span title="Left">L: {eff(weapon.leftEffect)}</span>
        <span title="Center">C: {eff(weapon.centerEffect)}</span>
        <span title="Right">R: {eff(weapon.rightEffect)}</span>
      </div>
      {weapon.description && (
        <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
          {weapon.description}
        </div>
      )}
    </div>
  );
}

export function EnemySelectPage({ page, dispatch, disabled }: Props) {
  if (page.isCommitted_) {
    return (
      <div className={styles.page}>
        <div className={styles.pageTitle}>Enemy</div>
        <div className={styles.committedBadge}>✓ Enemy configured</div>
      </div>
    );
  }

  const canConfirm = page.selectedWeaponId != null;

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle}>
        {page.isBoss ? `⚔ Boss: ${page.bossName}` : 'Configure Enemy'}
      </div>

      {page.bossDescription && (
        <div className={styles.narrative}>{page.bossDescription}</div>
      )}

      <div className={styles.infoRow}>
        <span>Charge Threshold: <strong style={{ color: 'var(--accent)' }}>{page.chargeThreshold}</strong></span>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Locked Weapons</div>
        <div className={eStyles.weaponGrid}>
          <WeaponCard weapon={page.lockedWeapon1} locked />
          <WeaponCard weapon={page.lockedWeapon2} locked />
        </div>
      </div>

      <div className={styles.section}>
        <div className={styles.sectionTitle}>Choose Third Weapon</div>
        <div className={eStyles.weaponGrid}>
          {page.weaponChoices.map(w => (
            <WeaponCard
              key={w.id}
              weapon={w}
              isSelected={page.selectedWeaponId === w.id}
              onClick={() => dispatch(SelectWeaponAction(w.id))}
              disabled={disabled}
            />
          ))}
        </div>
      </div>

      {page.abilityPool.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Enemy Abilities ({page.selectedAbilityIds.length} active)</div>
          <div className={styles.optionGrid}>
            {page.abilityPool.map(a => (
              <button
                key={a.id}
                className={`${styles.optionCard} ${page.selectedAbilityIds.includes(a.id) ? styles.selected : ''}`}
                onClick={() => dispatch(SelectEnemyAbilityAction(a.id))}
                disabled={disabled}
              >
                <span style={{ color: 'var(--orange)' }}>{a.name}</span>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginTop: '0.15rem' }}>{a.description}</div>
              </button>
            ))}
          </div>
        </div>
      )}

      <div className={styles.actionRow}>
        <button
          className={styles.primaryBtn}
          onClick={() => dispatch(ConfirmEnemyAction())}
          disabled={disabled || !canConfirm}
        >
          Prepare for Battle
        </button>
      </div>
    </div>
  );
}
