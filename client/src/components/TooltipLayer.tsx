import { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';

interface TipState { text: string; x: number; y: number; }

/** Renders a fixed-position tooltip for any element with data-tooltip="..." */
export function TooltipLayer() {
  const [tip, setTip] = useState<TipState | null>(null);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      const el = (e.target as Element | null)?.closest('[data-tooltip]');
      const text = el?.getAttribute('data-tooltip');
      if (text) setTip({ text, x: e.clientX, y: e.clientY });
      else setTip(null);
    };
    const onLeave = () => setTip(null);
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseleave', onLeave);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseleave', onLeave);
    };
  }, []);

  if (!tip) return null;

  // Keep within viewport: clamp left, appear above cursor
  const left = Math.min(tip.x + 14, window.innerWidth - 250);
  const top = tip.y - 8;

  return createPortal(
    <div style={{
      position: 'fixed',
      left,
      top,
      transform: 'translateY(-100%)',
      zIndex: 99999,
      background: '#16162a',
      border: '1px solid #4040608',
      borderColor: '#3a3a58',
      borderRadius: '4px',
      padding: '0.35em 0.7em',
      color: '#c8c8e0',
      fontSize: '0.73rem',
      lineHeight: '1.45',
      maxWidth: '230px',
      wordBreak: 'break-word',
      whiteSpace: 'pre-wrap',
      pointerEvents: 'none',
      boxShadow: '0 2px 8px rgba(0,0,0,0.5)',
    }}>
      {tip.text}
    </div>,
    document.body
  );
}
