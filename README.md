# Villa Nova

Projet Unity

---

## Structure du projet

```
Assets/
├── Content/        # Assets visuels et médias
│   ├── Models/         // Modèles 3D
│   ├── Materials/      // Matériaux et shaders
│   ├── Textures/       // Textures, lightmaps, HDRIs
│   ├── Audio/          // Musiques et effets sonores
│   └── Prefabs/        // Prefabs de scène
│
├── Data/           # Données configurables
│   ├── Camera/         // Configs de mode caméra
│   ├── Variables/      // Variables partagées
│   └── Settings/       // Paramètres globaux du projet
│
└── Scripts/        # Code source
    ├── Camera/         // Système de caméra FSM
    │   ├── CameraController.cs         // Point d'entrée, inputs, FSM
    │   ├── CameraModeConfig.cs         // Config serialisable par mode
    │   └── States/
    │       ├── CameraStartState.cs     // Animation d'intro
    │       ├── CameraMainState.cs      // Vue principale
    │       ├── CameraCloseState.cs     // Vue orthographique
    │       └── CameraFreeState.cs      // Free cam FPS
    ├── Core/           // Systèmes helper
    │   ├── FSM/            // FiniteStateMachine, State, Transition
    │   ├── Variables/      // ScriptableObject variables
    │   └── Extensions/     // Extensions Unity
    └── UI/             // Composants interface utilisateur
```

---

## Système Caméra

La caméra est pilotée avec quatre états :

| État | Touche | Description |
|------|--------|-------------|
| `Start` | — | Animation d'intro au lancement |
| `Main` | `1` | Orbite autour du centre de la carte |
| `Close` | `2` | Orbite rapprochée, projection orthographique |
| `Free` | `3` | Vol libre FPS |

### Free Cam : Contrôles
| Input | Action |
|-------|--------|
| `Z / S / Q / D` | Déplacement horizontal |
| `A` | Monter verticalement |
| `E` | Descendre verticalement |
| Clic droit (maintenu) | Regarder autour |
| Molette | Ajuster la vitesse de déplacement |
| `1 / 2 / 3` | Changer de mode caméra |

---

## Dépendances

- **Unity** 6000.3.10f1
