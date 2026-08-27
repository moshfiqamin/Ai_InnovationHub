// ============================================================
// FILE   : tailwind.config.js
// LAYER  : View — design system definition
// PURPOSE: Central design tokens (colour ramp, fonts, shadows,
//          keyframes). Defining them here rather than sprinkling
//          arbitrary values through components keeps the UI
//          consistent (NFR19) and easy to restyle later.
// ============================================================
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
      },
      colors: {
        // Primary brand ramp (teal) — matches the reference design's
        // buttons, links and logo mark.
        brand: {
          50:'#f0fdfa',100:'#ccfbf1',200:'#99f6e4',300:'#5eead4',400:'#2dd4bf',
          500:'#14b8a6',600:'#0d9488',700:'#0f766e',800:'#115e59',900:'#134e4a',950:'#042f2e',
        },
        // Secondary accent (amber) — the warm half of the logo mark
        accent: {
          400:'#fbbf24',500:'#f59e0b',600:'#d97706',700:'#b45309',
        },
        // Neutral surface ramp for dark sections
        ink: {
          800:'#1a1a2e',900:'#12121f',950:'#0a0a14',
        },
      },
      boxShadow: {
        glow: '0 0 40px -10px rgba(20,184,166,0.55)',
        card: '0 1px 3px rgba(16,24,40,0.06), 0 12px 32px -12px rgba(16,24,40,0.12)',
        lift: '0 8px 24px -6px rgba(13,148,136,0.28)',
      },
      keyframes: {
        'fade-up':   { '0%': { opacity:'0', transform:'translateY(14px)' }, '100%': { opacity:'1', transform:'translateY(0)' } },
        'float':     { '0%,100%': { transform:'translateY(0)' }, '50%': { transform:'translateY(-14px)' } },
        'drift':     { '0%,100%': { transform:'translate(0,0) scale(1)' }, '50%': { transform:'translate(28px,-22px) scale(1.08)' } },
        'shimmer':   { '0%': { backgroundPosition:'0% 50%' }, '100%': { backgroundPosition:'200% 50%' } },
      },
      animation: {
        'fade-up': 'fade-up .6s cubic-bezier(.21,.6,.35,1) both',
        'float':   'float 7s ease-in-out infinite',
        'drift':   'drift 16s ease-in-out infinite',
        'shimmer': 'shimmer 4s linear infinite',
      },
    },
  },
  plugins: [],
}
