using AltarHelperV2;

var cases = new (string Input, string Expected)[]
{
    ("-40% to Cold Resistance", "-#% to Cold Resistance"),
    ("+80% to Cold Resistance", "+#% to Cold Resistance"),
    ("25% increased Quantity of Items", "#% increased Quantity of Items"),
    ("1.6% chance to drop an additional Divine Orb", "#% chance to drop an additional Divine Orb"),
    ("Take 600 Chaos Damage every 2 seconds", "Take # Chaos Damage every # seconds"),
    ("(-60–-40)% to Lightning Resistance", "(-#–-#)% to Lightning Resistance"),
    ("2.8% chance to drop an additional [AbyssThread|Abyss Thread]", "#% chance to drop an additional Abyss Thread"),
    ("Final Boss drops 3 additional [BreachThread|Breach Threads]", "Final Boss drops # additional Breach Threads"),
    ("Enemies within 3.5 metres are Hindered, with 40% reduced Movement Speed", "Enemies within # metres are Hindered, with #% reduced Movement Speed"),
};

foreach (var (input, expected) in cases)
{
    var actual = AltarTextNormalizer.NormalizeNumbers(input);
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}' for '{input}'.");
}

Console.WriteLine($"Passed {cases.Length} altar text normalization tests.");
