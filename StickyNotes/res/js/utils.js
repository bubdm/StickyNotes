console.assert = function (cond) {
	if (!cond)
		console.error('assert error');
};