template N2Bitvec(n) {
	signal input in;
	signal output out[n];

	var lc1 = 0;
	var e2 = 1;
	for (var i = 0; i < n; i++) {
		out[i] <-- (in >> i) & 1;
		out[i] * (out[i] - 1) === 0; // alternatively, out[i] * out[i] = out[i]
		lc1 += out[i] * e2;
		e2 = 2*e2;
	}
	lc1 === in;
}

component main {public [in]} = N2Bitvec(32);