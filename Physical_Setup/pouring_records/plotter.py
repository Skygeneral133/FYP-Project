import pandas as pd
import matplotlib.pyplot as plt


def plot_pouring_data(
    csv_file,
    liquid_density_g_ml=1.0,
    subtract_initial_weight=False,
    save_path=None
):
    """
    Plot all pouring experiment measurements against elapsed time.

    Parameters
    ----------
    csv_file : str
        Path to the CSV file.
    liquid_density_g_ml : float
        Liquid density in g/mL. Use 1.0 for water.
    subtract_initial_weight : bool
        If True, removes the initial load-cell offset.
    save_path : str or None
        Optional output filename, for example "pouring_graphs.png".
    """

    df = pd.read_csv(csv_file)

    required_columns = {
        "elapsed_s",
        "angle_y_deg",
        "gy_deg_s",
        "weight_g",
        "pouring_rate_g_s"
    }

    missing = required_columns - set(df.columns)

    if missing:
        raise ValueError(f"Missing CSV columns: {sorted(missing)}")

    # Remove the initial load-cell offset if requested
    measured_weight = df["weight_g"].copy()

    if subtract_initial_weight:
        measured_weight = measured_weight - measured_weight.iloc[0]

    # Convert mass to volume: volume = mass / density
    df["volume_ml"] = measured_weight / liquid_density_g_ml

    plots = [
        ("angle_y_deg", "Container angle", "Angle (°)", "#3366CC"),
        ("gy_deg_s", "Angular velocity", "GY (°/s)", "#DC3912"),
        ("weight_g", "Measured weight", "Weight (g)", "#109618"),
        ("pouring_rate_g_s", "Pouring rate", "Rate (g/s)", "#FF9900"),
        ("volume_ml", "Estimated volume", "Volume (mL)", "#7B1FA2")
    ]

    fig, axes = plt.subplots(
        nrows=len(plots),
        ncols=1,
        figsize=(12, 11),
        sharex=True,
        layout="constrained"
    )

    for ax, (column, title, ylabel, colour) in zip(axes, plots):
        ax.plot(
            df["elapsed_s"],
            df[column],
            color=colour,
            linewidth=2,
            marker="o",
            markersize=3
        )

        # Put the measurement name above each graph
        ax.set_title(
            title,
            fontsize=11,
            fontweight="bold",
            loc="left",
            pad=5
        )

        # Keep the vertical label short
        ax.set_ylabel(ylabel, fontsize=10, labelpad=8)

        ax.grid(True, alpha=0.3)
        ax.tick_params(axis="both", labelsize=9)

    axes[-1].set_xlabel("Elapsed time (s)", fontsize=11, labelpad=8)

    fig.suptitle(
        "Pouring Experiment Data",
        fontsize=16,
        fontweight="bold"
    )

    plt.tight_layout(rect=[0, 0, 1, 0.98])

    if save_path:
        plt.savefig(save_path, dpi=300, bbox_inches="tight")
        print(f"Graph saved as: {save_path}")

    plt.show()

    return df

if __name__ == "__main__":
    from pathlib import Path

    folder = Path(__file__).parent
    csv_file = folder / "pouring_Deg_90_Volume_40_5.csv"
    output_file = folder / f"{csv_file.stem}_graphs.png"

    plot_pouring_data(
        csv_file=csv_file,
        liquid_density_g_ml=1.0,
        subtract_initial_weight=True,
        save_path=output_file
    )