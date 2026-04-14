import type { EpilogueRight, PlayerAction } from '../../api/types';
import styles from './shared.module.css';
import eStyles from './Epilogue.module.css';

interface Props {
  page: EpilogueRight;
  dispatch: (action: PlayerAction) => void;
  disabled: boolean;
}

export function EpiloguePage({ page, dispatch: _dispatch, disabled: _disabled }: Props) {
  const victory = page.chaptersCompleted >= 10;

  return (
    <div className={styles.page}>
      <div className={styles.pageTitle} style={{ color: victory ? 'var(--accent)' : 'var(--red)' }}>
        {victory ? '✦ Victory!' : '✗ Run Ended'}
      </div>

      <div className={eStyles.statGrid}>
        <StatBox label="Chapters" value={page.chaptersCompleted} />
        <StatBox label="Hull" value={page.finalHull} />
        <StatBox label="Coin" value={`⬡ ${page.finalCoin}`} />
        <StatBox label="Morale" value={`${page.finalMorale}%`} />
        <StatBox label="Damage Taken" value={page.totalDamageTaken} color="var(--red)" />
        <StatBox label="Shield Gen" value={page.totalShieldGenerated} color="var(--accent2)" />
        <StatBox label="Hack Gen" value={page.totalHackGenerated} color="var(--green)" />
        <StatBox label="Charge Gen" value={page.totalChargeGenerated} color="var(--orange)" />
        <StatBox label="Crew Deaths" value={page.totalCrewDeaths} color={page.totalCrewDeaths > 0 ? 'var(--red)' : 'var(--text-dim)'} />
        <StatBox label="Tears Used" value={page.totalTears} />
      </div>

      {page.survivorNames.length > 0 && (
        <div className={styles.section}>
          <div className={styles.sectionTitle}>Survivors</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.35rem' }}>
            {page.survivorNames.map(name => (
              <span key={name} className={styles.committedBadge}>{name}</span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatBox({ label, value, color }: { label: string; value: string | number; color?: string }) {
  return (
    <div className={eStyles.statBox}>
      <div className={eStyles.statLabel}>{label}</div>
      <div className={eStyles.statValue} style={{ color }}>{value}</div>
    </div>
  );
}
