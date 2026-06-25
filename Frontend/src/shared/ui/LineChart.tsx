type Point = { label: string; value: number };

type LineChartProps = {
  title: string;
  data: Point[];
  emptyLabel: string;
};

export function LineChart({ title, data, emptyLabel }: LineChartProps) {
  if (data.length === 0) {
    return (
      <div className="card text-center text-sm text-secondary">{emptyLabel}</div>
    );
  }

  const width = 320;
  const height = 140;
  const padX = 8;
  const padY = 12;
  const max = Math.max(...data.map((d) => d.value), 1);
  const step = (width - padX * 2) / Math.max(data.length - 1, 1);

  const points = data.map((d, i) => {
    const x = padX + i * step;
    const y = height - padY - (d.value / max) * (height - padY * 2);
    return `${x},${y}`;
  });

  const areaPoints = `${padX},${height - padY} ${points.join(" ")} ${padX + (data.length - 1) * step},${height - padY}`;

  return (
    <div className="card chart-card">
      <h3 className="chart-card-title">{title}</h3>
      <svg viewBox={`0 0 ${width} ${height}`} className="line-chart" role="img" aria-label={title}>
        <defs>
          <linearGradient id="chartFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--billflow-orange)" stopOpacity="0.35" />
            <stop offset="100%" stopColor="var(--billflow-orange)" stopOpacity="0.02" />
          </linearGradient>
        </defs>
        <polygon points={areaPoints} fill="url(#chartFill)" />
        <polyline
          points={points.join(" ")}
          fill="none"
          stroke="var(--billflow-orange)"
          strokeWidth="2.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        {data.map((d, i) => {
          const x = padX + i * step;
          const y = height - padY - (d.value / max) * (height - padY * 2);
          return <circle key={d.label} cx={x} cy={y} r="3.5" fill="var(--billflow-orange)" />;
        })}
      </svg>
      <div className="line-chart-labels">
        {data.map((d) => (
          <span key={d.label}>{d.label}</span>
        ))}
      </div>
    </div>
  );
}
