import * as env from "@env";
import * as Storage from "@storage";

function initDb(storage) {
	storage.root = {
		version: 1,
		dic_notes: {},
	};
}

let storage = Storage.open(env.home("file.db"));
let root = storage.root || initDb(storage);

class Settings {
	static root = root;

	commit() {
		storage.commit();
	}
}

export { Settings }