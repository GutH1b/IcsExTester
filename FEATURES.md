# IcsExTester – Features and Usage Guide

## Overview

`IcsExTester` is an automated differential testing framework for console programs.
It generates inputs, runs two executables with identical input, compares outputs,
measures execution time, and optionally performs memory analysis using Dr. Memory.

---

## Features

### Differential Output Testing
- Runs two executables on identical input
- Compares outputs line-by-line
- Detects missing lines, extra lines, and character-level differences
- Optional early termination on first mismatch

### Automated Random Test Generation
- Exercise-specific testers:
  - Ex4Tester
  - Ex5Tester
  - Ex6Tester
- Maintains internal state to prevent invalid inputs
- Produces realistic interactive command sequences

### Predefined Test Support
- Load tests from an external file
- `---` delimiter between tests
- Predefined tests run before random tests
- Inputs are preserved exactly as written

### Timeout Detection
- Configurable per-process timeout
- Independent timeout detection per executable
- Forced process termination on timeout

### Execution Time Measurement
- Wall-clock timing per executable
- Timing reused to estimate Dr. Memory timeout

### Memory Leak Detection (Optional)
- Integrates Dr. Memory
- Enabled only for selected exercises
- Reports only definite leaks
- Ignores possible leaks and zero-count summaries

### Failure Classification and Persistence
- Categorizes failures:
  - Output mismatch
  - Timeout
  - Definite memory leak
- Saves failing inputs to `Failures/`
- Supports informative (annotated) input

### Informative Input Tracing
- Annotated test generation
- Automatic sanitization before execution
- Optional printing and persistence for debugging

### Progress Bar (Failure-Only Mode)
- Live progress bar when normal output is suppressed
- Displays completion, rate, and ETA
- Cleared automatically on failure

### Robust Process Management
- Tracks spawned process IDs
- Ensures cleanup on exit, Ctrl+C, or crashes
- Prevents orphaned processes

### Config Versioning and Auto-Patching
- JSON-based `config.env`
- Missing keys auto-filled
- Unknown keys removed
- Config version automatically upgraded

---

## Usage Notes

- Configure executables and settings in `config.env`
- Select the exercise using `ExNum`
- Enable memory checks only when Dr. Memory is installed
- Use predefined tests for reproducible failures

---

## Intended Use Cases

- Comparing reference and student implementations
- Stress-testing interactive console programs
- Detecting memory management bugs
- Regression testing across randomized scenarios
