# 🌌 ZeroUniverse: Sovereign Industrial Computing Ecosystem

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Architecture: Dual-Tier](https://img.shields.io/badge/Architecture-Dual--Tier%20(Host%20%2B%20Firmware)-orange.svg)]()
[![Status: Active Development](https://img.shields.io/badge/Status-Active%20Development-brightgreen.svg)]()

**ZeroUniverse** is a sovereign, end-to-end industrial automation and computing ecosystem spanning from bare-metal microcontrollers (MCUs) to high-performance edge computing gateways, industrial vision systems, and SCADA/HMI management suites.

---

## 🏛️ Ecosystem Architecture & Tiers

ZeroUniverse is engineered with a **strict separation of operational domains**, decoupling deterministic hard real-time MCU execution from high-level data aggregation and user interface layers.

```mermaid
graph TD
    subgraph HostTier ["💻 ZeroPlatform (Host & Edge Tier)"]
        ZP_UI["ZeroUI / ZeroGraphics<br/><i>SCADA, HMI & Virtual Canvas</i>"]
        ZP_Pipe["ZeroPipeline / ZeroInference<br/><i>Edge AI & Inspection DAG</i>"]
        ZP_Data["ZeroData / ZeroStorage<br/><i>Gorilla TSDB & Columnar Engine</i>"]
        ZP_Comm["ZeroComm<br/><i>Industrial Master Protocol Engine</i>"]
    end

    subgraph WireProtocol ["🔌 ZeroWire / ZeroComm Wire Protocol"]
        Wire["Deterministic Framed Transport<br/><i>Modbus / Serial / CAN / Custom Binary FFI</i>"]
    end

    subgraph FirmwareTier ["⚡ ZeroEmbedded (Firmware & Silicon Tier)"]
        ZE_Rust["Rust Safety Island<br/><i>Memory Safety, State Machines, DSP</i>"]
        ZE_Core["C Compatibility Foundation<br/><i>Zero-cost Primitives, Span, Memory Pools</i>"]
        ZE_HAL["Type-safe HAL & Drivers<br/><i>ARM Cortex-M, RISC-V, SVD Codegen</i>"]
        ZE_Tooling["Clang Analyzer & Rules<br/><i>ISR/DMA Safety Context Verification</i>"]
    end

    %% Flow Connections
    HostTier <--> WireProtocol
    WireProtocol <--> FirmwareTier
```

---

## 📦 Core Subsystems

### 1. [ZeroPlatform](file:///e:/15.%20Other/ZeroUniverse/ZeroPlatform) (The Sovereign .NET Industrial Suite)
- **Tech Stack**: 100% Pure C# (.NET 8.0, .NET Framework 4.6.2, .NET Standard 2.0).
- **Core Pillars**: 12 modular subsystems (ZeroTensor, ZeroCompute, ZeroData, ZeroStorage, ZeroInference, ZeroNeural, ZeroSignal, ZeroGeometry, ZeroComm, ZeroGraphics, ZeroUI, ZeroPipeline).
- **Domain**: Edge AI, computer vision, analytical SDF cards, 60 FPS oscilloscope waveforms, 10M+ row virtual grid, time-series storage.

### 2. [ZeroEmbedded](file:///e:/15.%20Other/ZeroUniverse/ZeroEmbedded) (Hybrid C + Rust Embedded Framework & Toolchain)
- **Tech Stack**: Hybrid C99/C11 Foundation + Rust (`#![no_std]`) + LLVM/Clang Tooling.
- **Core Pillars**:
  - Zero-cost memory safety primitives (`fw_span_t`, memory pool, arena, ownership).
  - Rust safety island for complex protocols, algorithms, and state machines.
  - Clang static analysis passes for ISR context validation and DMA buffer lifetime enforcement.
  - CMSIS-SVD-driven type-safe HAL generator.
- **Domain**: Microcontrollers (ARM Cortex-M, RISC-V, AVR), bare-metal, RTOS adapters (FreeRTOS, Zephyr).

---

## 🚀 Quick Navigation

- **Host Development (.NET)**: Navigate to [`ZeroPlatform/`](file:///e:/15.%20Other/ZeroUniverse/ZeroPlatform) and open `ZeroPlatform.slnx`.
- **Firmware Development (C/Rust)**: Navigate to [`ZeroEmbedded/`](file:///e:/15.%20Other/ZeroUniverse/ZeroEmbedded) to explore the Core C primitives and Rust safety crates.

---

## 📄 Licensing & Governance

All platforms and libraries within the **ZeroUniverse** ecosystem are authored and architected by **Phong Võ** (`kzxl`) and released under the permissive **MIT License**.
