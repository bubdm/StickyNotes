import * as env from "@env";
import * as sys from "@sys";
import * as sciter from "@sciter";

function initDb() {
	let root = {
		version: 1,
		dic_notes: {},
	};
	return root;
}

const PATH = document.url('storage.json').substr(7);

class Settings {
	static root;

	static setup() {
		let read = Window.this.xcall("Host_ReadFile", PATH);
		let data = eval('(' + read + ')') || initDb();
		Settings.root = data;
		console.log(JSON.stringify(Settings.root));
	}

	static commit() {
		console.log(JSON.stringify(Settings.root));
		Window.this.xcall("Host_WriteFile", PATH, JSON.stringify(Settings.root))
	}
}

Settings.setup();

export { Settings }