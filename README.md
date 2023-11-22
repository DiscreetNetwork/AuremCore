# Aurem

Aurem is a confidential proof-of-stake ("CPoS") mechanism that builds on the work of Cardinal Cryptography's [AlephBFT](https://arxiv.org/abs/1908.05156) and heavily improves upon the mechanism in [this paper](https://eprint.iacr.org/2018/1105.pdf).

Aurem is a combination of three protocols to allow for decentralized consensus on the Discreet distributed ledger by splitting up consensus into three separate problems:
- Selecting a sub-committee of validators in the network to finalize transactions for a given epoch;
- Selecting which validator in a consensus committee should mint the next block; and
- Achieving a total ordering of consensus communication and blocks to finalize transactions.

This contains several projects related to Aurem. Each one serves as a standalone library and provides useful primitives and APIs for use in testing and production code.

#### WIP REPOSITORY – CODE IS PRESENTED “AS IS”. Please refer to the [roadmap](https://discreet.net/roadmap) for progress.
