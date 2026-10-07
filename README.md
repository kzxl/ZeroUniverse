# 🌌 ZeroUniverse: Sovereign Industrial Computing Ecosystem

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![ZeroPlatform](https://img.shields.io/badge/ZeroPlatform-40%20Subsystems-blue.svg)](https://github.com/kzxl/ZeroPlatform)
[![ZeroRust](https://img.shields.io/badge/ZeroRust-Hard%20Real--Time%20Fieldbus-orange.svg)](https://github.com/kzxl/ZeroRust)
[![Zero External Dependencies](https://img.shields.io/badge/Dependencies-0%20External-brightgreen.svg)]()
[![Status: Active Development](https://img.shields.io/badge/Status-Active%20Development-brightgreen.svg)]()

**ZeroUniverse** is a sovereign, end-to-end industrial automation and computing ecosystem spanning from bare-metal microcontrollers (MCUs) to sub-millisecond real-time robotics, high-performance edge gateways, machine vision systems, and hardware-accelerated SCADA/HMI management suites.

---

## 🏛️ Ecosystem Architecture

ZeroUniverse decouples deterministic, hard real-time silicon execution and fieldbus control from high-level data aggregation and visual telemetry via a sovereign multi-language topology bridged by the **ZeroWire** and high-speed IPC protocols:

```
                       ┌────────────────────────────────────────────────────────┐
                       │               ZeroPlatform (.NET 8/9/10)               │
                       │   Desktop HMI • SCADA • Distributed Cloud • Vision     │
                       └───────────────────────────▲────────────────────────────┘
                                                   │ IPC / ZeroMQ / gRPC
                                                   ▼
┌────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                       ZeroRust (`zero-rs`)                                     │
├───────────────────────────────┬────────────────────────────────┬───────────────────────────────┤
│          zero-core            │           zero-bus             │          zero-motion          │
│ • #![no_std] primitives      │ • Real-time CAN 2.0 / CAN-FD   │ • 6-DOF / SCARA / Cartesian   │
│ • Lock-free SPSC RingBuffer   │ • CANopen CiA 301 (NMT/SDO/PDO)│ • Forward & Inverse Kinematics│
│ • Zero-alloc Binary Parsing   │ • CiA 402 Servo Drive Profile  │ • Geometric Jacobian Matrix   │
│ • Q16.16/Q32.32 Fixed Point   │ • Micro-second Cyclic Sync     │ • 7-Phase Jerk-Limited S-Curve│
├───────────────────────────────┼────────────────────────────────┼───────────────────────────────┤
│           zero-hal            │            zero-dsp            │          zero-modbus          │
│ • Digital I/O & Debouncing    │ • 2nd-Order Butterworth IIR    │ • Modbus RTU (Serial/RS485)   │
│ • 4x Quadrature Decoder       │ • Moving Average (Const-size)  │ • Modbus TCP (MBAP Header)    │
│ • Step/Dir DDA Pulse Train    │ • 1D Optimal Kalman Filter     │ • Tableless & Fast CRC16      │
│ • Hardware Mock Abstractions  │ • Radix-2 FFT Vibration Peak   │ • Zero-alloc PDU Serialization│
└───────────────────────────────┴────────────────────────────────┴───────────────────────────────┘
                                                   ▲
                                                   │ SPI / CAN / UART
                                                   ▼
                       ┌────────────────────────────────────────────────────────┐
                       │               ZeroEmbedded (C / Firmware)              │
                       │  Bare-Metal MCUs • Type-State HAL • Ultra-Low Latency  │
                       └────────────────────────────────────────────────────────┘
```

- **Host & Edge Computing Tier (`ZeroPlatform`)**: 100% pure C# industrial PC / SCADA framework (40 autonomous subsystems).
- **Hard Real-Time Robotics & Fieldbus Tier (`ZeroRust`)**: Sovereign `#![no_std]` Rust suite for sub-millisecond robotics kinematics, CANopen/CiA 402, and edge DSP.
- **Silicon & Firmware Tier (`ZeroEmbedded`)**: Deterministic `#![no_std]` C + Rust real-time MCU framework.
- **Sovereign Desktop Application Suite (`ZeroApps`)**: Native high-performance utilities and developer tools (`ZStack`, `ZVision`, `ZShield`, etc.).
- **Interconnect Protocol (`ZeroWire`)**: Noise-resilient, zero-allocation binary transport.

👉 **[Read the Full Architecture & Operational Tiers Specification](docs/architect/ecosystem-architecture.md)**

---

## 📦 Primary Ecosystem Tiers & Repositories

| Tier | Repository | Tech Stack | Core Capabilities |
| :--- | :--- | :--- | :--- |
| **Host & Edge** | **[`kzxl/ZeroPlatform`](https://github.com/kzxl/ZeroPlatform)** | **100% Pure C#**<br/>*(.NET 8.0, 4.6.2, Standard 2.0)* | **40 sovereign subsystems**: HMI/SCADA (`ZeroUI`), Direct3D 11 (`ZeroGraphics`), Cryptography & Cyber-Defense (`ZeroSecurity`), TSDB (`ZeroStorage`), IoT Protocols (`ZeroIoT`), Vector PDF (`ZeroReports`), Acoustics (`ZeroAudioVisual`), 3D Digital Twin (`ZeroTwin3D`), Autonomous AI Agent (`ZeroAgent`), Industrial Fieldbus (`ZeroBus`), Motion & Kinematics (`ZeroMotion`), 3D SLAM (`ZeroScan3D`), etc. |
| **Real-Time Robotics & Fieldbus** | **[`kzxl/ZeroRust`](https://github.com/kzxl/ZeroRust)** | **Pure Rust**<br/>*(Strictly `#![no_std]`, Zero GC)* | **6 real-time crates**: `zero-core` (Lock-free SPSC buffer, Q16.16 math), `zero-bus` (CAN 2.0/FD, CANopen CiA 301, CiA 402 Servo Profile), `zero-motion` (SCARA/6-DOF Kinematics, 7-Phase Jerk-Limited S-Curve), `zero-hal` (Debouncer, 4x Quadrature Decoder, Step/Dir DDA), `zero-dsp` (Butterworth IIR, Kalman, Radix-2 FFT), `zero-modbus` (RTU/TCP). |
| **Silicon & Firmware** | **[`kzxl/ZeroEmbedded`](https://github.com/kzxl/ZeroEmbedded)** | **Hybrid C99/C11 + Rust**<br/>*(Strictly `#![no_std]`, Zero GC/VM)* | Zero-cost memory safety (`fw_span_t`, pool, arena), lockless SPSC queues, Type-State peripheral drivers, DMA ownership tokens, and compile-time ISR context analyzer. |
| **Desktop App Suite** | **`ZeroApps`** | **C# .NET 8 / 9 / 10 WPF & WinForms** | Sovereign consumer & industrial utilities: `ZClean`, `ZDoc`, `ZProbe`, `ZRecover`, `ZShield` (Industrial EDR + UEBA), `ZStack` (Computational Imaging & Focus Stacking Studio), `ZSync`, `ZTalk`, `ZTranslate`, `ZVision`, `ZWall`, and `ZZip`. |
| **Interconnect** | **`ZeroWire` Protocol** | **Shared C-ABI & C# Engine** | Deterministic binary framing with CRC16-CCITT integrity, sliding-window stream resynchronization, and zero-allocation framing. |

---

## ⚡ Performance Highlights Across the Universe

- **Host Tier (`ZeroPlatform`)**: 60 FPS oscilloscope waveforms, 10M+ rows virtual SCADA grid, 1.37 bytes/sample TSDB compression, 40 autonomous subsystems with 100% test pass.
- **Real-Time Tier (`ZeroRust`)**: Sub-microsecond deterministic execution, zero heap allocation on motion hot-paths, 23 unit tests passed (100%), pure `#![no_std]` bare-metal compatibility.
- **Firmware Tier (`ZeroEmbedded`)**: 0.946x zero-cost memory abstraction overhead, 20.96 ns/alloc memory pool with double-free protection, 163.5 Million Ops/sec lock-free SPSC queues, 114 unit tests passed (100%).

---

## 🚀 Quick Start Navigation

### Host Development (.NET)
```bash
cd ZeroPlatform
.\clone-ecosystem.ps1
dotnet build ZeroPlatform.slnx -c Release
dotnet test ZeroPlatform.slnx
```

### Real-Time Robotics & Fieldbus (Rust)
```bash
cd ZeroRust
cargo test --workspace
cargo clippy --workspace --all-targets -- -D warnings
```

### Firmware Development (C + Rust)
```bash
cd ZeroEmbedded
cl /nologo /W4 /WX /O2 /I core-c/include core-c/src/*.c tests/test_core_memory.c /Fe:test_hardened.exe
.\test_hardened.exe
```

---

## 📄 Authors & License

Architected and developed by **Phong Võ** (`kzxl`). Released under the permissive **MIT License**.
