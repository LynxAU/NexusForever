# Spell Data-Driven Report

Generated: 2026-02-27 04:20:38 UTC
Table source: `C:\Games\Dev\WIldstar\NexusForever\tmp\tables\tbl`

## Coverage Snapshot
- Spell4Effects rows: **131010**
- Unique effect types present: **134**
- Periodic Damage rows (`Damage` with tick/duration): **2148**
- Periodic Heal rows (`Heal` with tick/duration): **715**
- Periodic Shield-Heal rows (`HealShields` with tick/duration): **29**
- CC apply rows (`CCStateSet`): **7185**
- CC break rows (`CCStateBreak`): **420**
- Dispel rows (`SpellDispel`): **168**
- Proc rows (`Proc`): **3498**
- Referenced CC condition rows (`Spell4CCConditions` used by Spell4): **12**

## Effect Metadata (from SpellEffectType.tbl)
| EffectType | Id | Flags | DataTypes (00..09) |
|---|---:|---:|---|
| Damage | 8 | 23 | 0,2,2,0,0,0,2,2,2,2 |
| Heal | 10 | 23 | 0,2,2,2,2,2,2,2,2,2 |
| HealShields | 118 | 23 | 0,2,2,2,2,2,2,2,2,2 |
| CCStateSet | 4 | 29 | 2,0,2,1,2,2,2,2,1,2 |
| CCStateBreak | 27 | 21 | 2,2,2,2,2,2,2,2,2,2 |
| SpellDispel | 53 | 21 | 2,2,2,2,2,2,2,2,2,2 |
| UnitPropertyModifier | 11 | 31 | 2,2,UInt,UInt,UInt,2,2,2,2,2 |

## Periodic Tick Patterns (Top 20)
| EffectType | Tick(ms) | Duration(ms) | Rows |
|---|---:|---:|---:|
| Damage | 2000 | 8000 | 1078 |
| Proxy | 5000 | 60000 | 240 |
| Heal | 1000 | 10000 | 222 |
| Damage | 3000 | 15000 | 130 |
| Damage | 2000 | 12000 | 124 |
| Heal | 2000 | 10000 | 99 |
| UnitPropertyModifier | 3000 | 15000 | 96 |
| Proxy | 5000 | 30000 | 74 |
| Damage | 1000 | 4000 | 73 |
| Heal | 1000 | 5000 | 72 |
| Damage | 2000 | 10000 | 70 |
| Damage | 1000 | 10000 | 69 |
| Damage | 1000 | 5000 | 67 |
| Heal | 2000 | 12000 | 66 |
| Proxy | 1000 | 60000 | 63 |
| Heal | 3000 | 12000 | 53 |
| Damage | 2000 | 9000 | 48 |
| Proxy | 500 | 6000 | 44 |
| Proxy | 1000 | 6000 | 31 |
| VitalModifier | 2000 | 10000 | 29 |

## Periodic Flags/Targets (Top 20)
| EffectType | Flags | TargetFlags | Rows |
|---|---:|---:|---:|
| Damage | 0 | 2 | 1499 |
| Proxy | 0 | 1 | 376 |
| Heal | 0 | 2 | 207 |
| Proxy | 0 | 2 | 177 |
| Heal | 0 | 6 | 171 |
| Proxy | 0 | 6 | 163 |
| Damage | 1 | 2 | 144 |
| Damage | 0 | 4 | 120 |
| UnitPropertyModifier | 0 | 2 | 98 |
| VitalModifier | 0 | 1 | 89 |
| Heal | 512 | 5 | 75 |
| Heal | 0 | 1 | 67 |
| Damage | 512 | 2 | 59 |
| Damage | 16 | 2 | 51 |
| Heal | 512 | 2 | 51 |
| Damage | 512 | 4 | 34 |
| VitalModifier | 0 | 2 | 33 |
| Damage | 0 | 6 | 29 |
| RavelSignal | 0 | 1 | 26 |
| Heal | 524304 | 1 | 25 |

## CCStateSet Distribution
| DataBits00 | StateName | Rows | DefaultDRId |
|---:|---|---:|---:|
| 8 | Knockdown | 2291 | 8 |
| 0 | Stun | 1482 | 3 |
| 21 | Snare | 604 | 0 |
| 3 | Disarm | 398 | 0 |
| 16 | Knockback | 369 | 14 |
| 2 | Root | 262 | 4 |
| 9 | Vulnerability | 205 | 0 |
| 11 | Disorient | 201 | 11 |
| 12 | Disable | 199 | 0 |
| 15 | Blind | 187 | 6 |
| 20 | Tether | 105 | 9 |
| 13 | Taunt | 104 | 10 |
| 27 | AbilityRestriction | 93 | 0 |
| 6 | Fear | 85 | 0 |
| 4 | Silence | 85 | 0 |
| 24 | Subdue | 76 | 12 |
| 7 | Hold | 72 | 0 |
| 22 | Interrupt | 61 | 0 |
| 18 | Pull | 60 | 13 |
| 17 | Pushback | 47 | 17 |
| 14 | DeTaunt | 38 | 5 |
| 1 | Sleep | 36 | 0 |
| 26 | DisableCinematic | 34 | 0 |
| 10 | VulnerabilityWithAct | 29 | 0 |

## CCStateBreak Distribution
| DataBits00 | Interpretation | Rows |
|---:|---|---:|
| 2 | Root | 229 |
| 28371455 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Disorient,Taunt,DeTaunt,Blind,Tether,Snare,Daze,Subdue] | 29 |
| 28372991 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Vulnerability,VulnerabilityWithAct,Disorient,Taunt,DeTaunt,Blind,Tether,Snare,Daze,Subdue] | 14 |
| 28870917 | Mask[Stun,Root,Knockdown,Disorient,Blind,PositionSwitch,Tether,Snare,Daze,Subdue] | 14 |
| 0 | Stun | 12 |
| 28879109 | Mask[Stun,Root,Knockdown,Disorient,Taunt,Blind,PositionSwitch,Tether,Snare,Daze,Subdue] | 10 |
| 19984383 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Vulnerability,VulnerabilityWithAct,Disorient,Taunt,DeTaunt,Blind,Tether,Snare,Subdue] | 10 |
| 2097156 | Mask[Root,Snare] | 10 |
| 19974655 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Disorient,DeTaunt,Blind,Tether,Snare,Subdue] | 9 |
| 33548677 | Mask[Stun,Root,Hold,Knockdown,Disorient,Taunt,DeTaunt,Blind,Knockback,Pushback,Pull,PositionSwitch,Tether,Snare,Interrupt,Daze,Subdue] | 9 |
| 8 | Knockdown | 8 |
| 3145860 | Mask[Root,Hold,Tether,Snare] | 6 |
| 4 | Silence | 5 |
| 5 | Polymorph | 5 |
| 4096 | Mask[Disable] | 5 |
| 3145732 | Mask[Root,Tether,Snare] | 5 |
| 1 | Sleep | 3 |
| 3205380 | Mask[Root,Knockdown,Disorient,Taunt,DeTaunt,Blind,Tether,Snare] | 3 |
| 39423 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Disorient,Disable,Blind] | 3 |
| 19974527 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Knockdown,Disorient,DeTaunt,Blind,Tether,Snare,Subdue] | 2 |
| 3 | Disarm | 2 |
| 6 | Fear | 2 |
| 7 | Hold | 2 |
| 268435455 | Mask[Stun,Sleep,Root,Disarm,Silence,Polymorph,Fear,Hold,Knockdown,Vulnerability,VulnerabilityWithAct,Disorient,Disable,Taunt,DeTaunt,Blind,Knockback,Pushback,Pull,PositionSwitch,Tether,Snare,Interrupt,Daze,Subdue,Grounded,DisableCinematic,AbilityRestriction] | 2 |

## CCStateBreak Shape
- Single-state payload rows: **270**
- Mask payload rows: **150**
- Unknown payload rows: **0**

## Stack Group Signals
- Spells with a stack group id: **44741**
- Periodic spells with stack group id: **3740**

| StackTypeEnum | StackCap | SpellCount |
|---:|---:|---:|
| 4 | 0 | 26471 |
| 0 | 1 | 7140 |
| 1 | 1 | 6140 |
| 1 | 5 | 770 |
| 5 | 0 | 703 |
| 2 | 1 | 636 |
| 1 | 200 | 391 |
| 0 | 999 | 367 |
| 0 | 5 | 262 |
| 0 | 3 | 223 |
| 3 | 1 | 201 |
| 1 | 3 | 193 |
| 0 | 10 | 185 |
| 1 | 10 | 158 |
| 1 | 4 | 121 |
| 1 | 2 | 117 |
| 0 | 2 | 59 |
| 3 | 10 | 59 |
| 0 | 200 | 56 |
| 0 | 100 | 55 |

## SpellDispel Payload Patterns (Top 20)
| DataBits00 | DataBits01 | DataBits02 | DataBits03 | DataBits04 | Flags | TargetFlags | Rows |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 3 | 3 | 36 | 0 | 0 | 2 | 31 |
| 2 | 2 | 0 | 38 | 0 | 0 | 5 | 16 |
| 1 | 1 | 2 | 36 | 0 | 0 | 2 | 10 |
| 1 | 1 | 0 | 36 | 0 | 1 | 4 | 8 |
| 1 | 1 | 0 | 38 | 1 | 0 | 1 | 8 |
| 1 | 1 | 2 | 38 | 0 | 0 | 4 | 8 |
| 50 | 50 | 1 | 38 | 0 | 0 | 1 | 6 |
| 1 | 1 | 0 | 36 | 1 | 0 | 4 | 5 |
| 3 | 3 | 0 | 38 | 0 | 1 | 5 | 5 |
| 50 | 128 | 1 | 38 | 0 | 0 | 1 | 5 |
| 1 | 1 | 0 | 38 | 0 | 0 | 1 | 5 |
| 2 | 2 | 0 | 38 | 0 | 1 | 5 | 4 |
| 1 | 1 | 1 | 36 | 0 | 0 | 2 | 3 |
| 1 | 1 | 1 | 38 | 1 | 0 | 1 | 3 |
| 1 | 3 | 2 | 36 | 0 | 0 | 1 | 3 |
| 4294967295 | 4294967295 | 1 | 38 | 0 | 0 | 1 | 3 |
| 0 | 100 | 2 | 36 | 0 | 0 | 5 | 3 |
| 0 | 100 | 2 | 38 | 0 | 0 | 5 | 3 |
| 3 | 3 | 1 | 36 | 0 | 0 | 2 | 2 |
| 1 | 1 | 2 | 38 | 0 | 0 | 2 | 2 |

## Proc Payload Patterns (Top 25)
| DataBits00 | DataBits01 | DataBits02 | DataBits03 | DataBits04 | Flags | TargetFlags | Tick(ms) | Duration(ms) | Rows |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 16 | 22930 | 1065353216 | 2 | 4294967295 | 0 | 1 | 0 | 5000 | 29 |
| 11 | 31317 | 1065353216 | 1 | 4294967295 | 2 | 1 | 0 | 0 | 24 |
| 10 | 31317 | 1065353216 | 1 | 4294967295 | 2 | 1 | 0 | 0 | 22 |
| 16 | 43565 | 1065353216 | 2 | 1000 | 0 | 1 | 0 | 4000 | 16 |
| 10 | 32519 | 1065353216 | 2 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 12 | 32843 | 1065353216 | 4 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 103 | 35883 | 1065353216 | 1 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 115 | 37549 | 1065353216 | 2 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 12 | 37725 | 1036831949 | 4 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 10 | 38581 | 1065353216 | 2 | 4294967295 | 2 | 1 | 0 | 0 | 15 |
| 16 | 43565 | 1065353216 | 2 | 1000 | 0 | 1 | 0 | 5000 | 13 |
| 249 | 57852 | 1065353216 | 12 | 1000 | 0 | 1 | 0 | 5000 | 9 |
| 21 | 79085 | 1065353216 | 18 | 10000 | 2 | 2 | 0 | 0 | 9 |
| 21 | 79085 | 1065353216 | 10 | 10000 | 2 | 2 | 0 | 0 | 9 |
| 12 | 82574 | 1065353216 | 12 | 500 | 2 | 5 | 0 | 0 | 9 |
| 16 | 88335 | 1065353216 | 12 | 1 | 0 | 4 | 0 | 4000 | 9 |
| 23 | 82492 | 1065353216 | 12 | 1000 | 2 | 1 | 0 | 0 | 8 |
| 53 | 5659 | 1065353216 | 2 | 4294967295 | 512 | 1 | 0 | 1000 | 7 |
| 15 | 2986 | 1065353216 | 4 | 4294967295 | 2 | 1 | 0 | 0 | 7 |
| 16 | 22930 | 1065353216 | 2 | 4294967295 | 0 | 1 | 0 | 3000 | 7 |
| 40 | 39329 | 1065353216 | 2 | 5000 | 2 | 2 | 0 | 0 | 7 |
| 16 | 22930 | 1065353216 | 2 | 4294967295 | 0 | 1 | 0 | 4000 | 6 |
| 10 | 48845 | 1065353216 | 9 | 1250 | 2 | 1 | 0 | 0 | 6 |
| 16 | 62686 | 1065353216 | 20 | 1000 | 2 | 2 | 0 | 0 | 6 |
| 16 | 43565 | 1065353216 | 2 | 1000 | 0 | 1 | 0 | 10000 | 6 |

## Periodic SpellClass Mix
| SpellClass (raw) | ClassBucket | SpellCount |
|---:|---|---:|
| 39 | DebuffNonDispellable | 1321 |
| 38 | DebuffDispellable | 764 |
| 36 | BuffDispellable | 602 |
| 37 | BuffNonDispellable | 522 |
| 40 | Other | 448 |
| 14 | Other | 82 |
| 0 | Other | 1 |

## CC Cast-Condition Masks (Top 20)
| CcStateMask(hex) | DecodedMaskStates | Required(hex) | DecodedRequiredStates | Rows |
|---|---|---|---|---:|
| 0x0D0D1363 | Stun,Sleep,Polymorph,Fear,Knockdown,Vulnerability,Disable,Knockback,Pull,PositionSwitch,Subdue,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x0D0D136B | Stun,Sleep,Disarm,Polymorph,Fear,Knockdown,Vulnerability,Disable,Knockback,Pull,PositionSwitch,Subdue,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x0D0D1373 | Stun,Sleep,Silence,Polymorph,Fear,Knockdown,Vulnerability,Disable,Knockback,Pull,PositionSwitch,Subdue,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x0D1C13DF | Stun,Sleep,Root,Disarm,Silence,Fear,Hold,Knockdown,Vulnerability,Disable,Pull,PositionSwitch,Tether,Subdue,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x040C12C7 | Stun,Sleep,Root,Fear,Hold,Vulnerability,Disable,Pull,PositionSwitch,DisableCinematic | 0x00000000 |  | 1 |
| 0x0C4D1363 | Stun,Sleep,Polymorph,Fear,Knockdown,Vulnerability,Disable,Knockback,Pull,PositionSwitch,Interrupt,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x0C001080 | Hold,Disable,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x0D0C13C7 | Stun,Sleep,Root,Fear,Hold,Knockdown,Vulnerability,Disable,Pull,PositionSwitch,Subdue,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x00000200 | Vulnerability | 0x00000000 |  | 1 |
| 0x0C4D9363 | Stun,Sleep,Polymorph,Fear,Knockdown,Vulnerability,Disable,Blind,Knockback,Pull,PositionSwitch,Interrupt,DisableCinematic,AbilityRestriction | 0x00000000 |  | 1 |
| 0x050D1363 | Stun,Sleep,Polymorph,Fear,Knockdown,Vulnerability,Disable,Knockback,Pull,PositionSwitch,Subdue,DisableCinematic | 0x00000000 |  | 1 |

## Implementation Guidance (Data-Driven)
- Prioritize periodic support for `Damage`, `Heal`, and `HealShields` rows with high-frequency tick/duration patterns above.
- Implement CC duration using `CCStateSet` rows (`DataBits00` state + `DurationTime`) and tie DR buckets via `CCStates.CcStateDiminishingReturnsId`.
- Expand dispel from CC-only to buff/debuff instance removal by honoring `SpellClass` and stack-group metadata.
- Apply stack semantics using `Spell4StackGroup` (`StackTypeEnum`, `StackCap`) for periodic auras.
- Use `Spell4CCConditions` masks/required flags for cast gating and persistence checks.
