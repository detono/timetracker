/**
 * The handful of outline icons the app uses, drawn inline so there's no icon dependency.
 * They take the text color and size of their surroundings.
 */
const paths = {
  clock: "M12 7v5l3 2M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0",
  team: "M9 7m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2M16 3.13a4 4 0 0 1 0 7.75M21 21v-2a4 4 0 0 0 -3 -3.85",
  leave: "M5 21h14M12 21v-9M12 12c-3 0 -6 1 -8 3c1 -5 4 -8 8 -8s7 3 8 8c-2 -2 -5 -3 -8 -3M12 4v3",
  reports: "M3 13a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1zM15 9a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v10a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1zM9 5a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1zM4 20h14",
  people: "M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2",
  tag: "M7.5 7.5m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0M3 6v5.17a2 2 0 0 0 .59 1.42l7.71 7.71a2.41 2.41 0 0 0 3.41 0l5.59 -5.59a2.41 2.41 0 0 0 0 -3.41l-7.71 -7.71a2 2 0 0 0 -1.42 -.59h-5.17a3 3 0 0 0 -3 3z",
  folder: "M5 4h4l3 3h7a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2",
  building: "M3 21h18M9 8h1M9 12h1M9 16h1M14 8h1M14 12h1M14 16h1M5 21v-16a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v16",
  shield: "M12 3a12 12 0 0 0 8.5 3a12 12 0 0 1 -8.5 15a12 12 0 0 1 -8.5 -15a12 12 0 0 0 8.5 -3",
  menu: "M4 6h16M4 12h16M4 18h16",
  close: "M18 6l-12 12M6 6l12 12",
  logout: "M14 8v-2a2 2 0 0 0 -2 -2h-7a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h7a2 2 0 0 0 2 -2v-2M9 12h12l-3 -3M18 15l3 -3",
  dots: "M5 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0M12 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0M19 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
  lock: "M5 13a2 2 0 0 1 2 -2h10a2 2 0 0 1 2 2v6a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2zM11 16a1 1 0 1 0 2 0a1 1 0 0 0 -2 0M8 11v-4a4 4 0 1 1 8 0v4"
} as const;

export type IconName = keyof typeof paths;

export function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  return (
    <svg
      className="icon"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      <path d={paths[name]} />
    </svg>
  );
}
