
const databases = {};

window.initSqliteWasm = async function () {
    if (!window.SQL) {
        console.log("Loading SQLite WASM...");

        const sqliteScript = document.createElement('script');
        sqliteScript.src = 'https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/sql-wasm.js';
        sqliteScript.async = true;

        await new Promise((resolve, reject) => {
            sqliteScript.onload = resolve;
            sqliteScript.onerror = reject;
            document.head.appendChild(sqliteScript);
        });

        window.SQL = await initSqlJs({
            locateFile: file => `https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.8.0/${file}`
        });

        console.log("SQLite WASM loaded successfully");
    }
};

window.createDatabase = async function (dbName) {
    if (!databases[dbName]) {
        try {
            const existingData = await loadFromIndexedDB(dbName);

            if (existingData) {
                databases[dbName] = new window.SQL.Database(existingData);
                console.log(`Loaded existing database: ${dbName}`);
            } else {
                databases[dbName] = new window.SQL.Database();
                console.log(`Created new database: ${dbName}`);
            }
        } catch (e) {
            console.error(`Error creating/opening database ${dbName}:`, e);
            databases[dbName] = new window.SQL.Database();
        }
    }
    return true;
};

window.executeQuery = function (dbName, sql, params = []) {
    try {
        const db = databases[dbName];
        if (!db) {
            console.error(`Database ${dbName} not found`);
            return [];
        }

        const stmt = db.prepare(sql);

        const results = [];
        if (params && params.length > 0) {
            stmt.bind(params);
        }

        while (stmt.step()) {
            results.push(stmt.getAsObject());
        }

        stmt.free();

        saveToIndexedDB(dbName, db.export());

        return results;
    } catch (e) {
        console.error(`Error executing query: ${sql}`, e);
        return [];
    }
};

window.executeNonQuery = function (dbName, sql, params = []) {
    try {
        const db = databases[dbName];
        if (!db) {
            console.error(`Database ${dbName} not found`);
            return -1;
        }

        const result = db.run(sql, params);

        saveToIndexedDB(dbName, db.export());

        return result.changes || 0;
    } catch (e) {
        console.error(`Error executing non-query: ${sql}`, e);
        return -1;
    }
};

async function saveToIndexedDB(dbName, data) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('GoweliDatabase', 1);

        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('databases')) {
                db.createObjectStore('databases', { keyPath: 'name' });
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            const transaction = db.transaction(['databases'], 'readwrite');
            const store = transaction.objectStore('databases');

            store.put({ name: dbName, data: data });

            transaction.oncomplete = function () {
                resolve();
            };

            transaction.onerror = function (event) {
                reject(event.target.error);
            };
        };

        request.onerror = function (event) {
            reject(event.target.error);
        };
    });
}

async function loadFromIndexedDB(dbName) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('GoweliDatabase', 1);

        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('databases')) {
                db.createObjectStore('databases', { keyPath: 'name' });
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('databases')) {
                resolve(null);
                return;
            }

            const transaction = db.transaction(['databases'], 'readonly');
            const store = transaction.objectStore('databases');
            const getRequest = store.get(dbName);

            getRequest.onsuccess = function (event) {
                if (event.target.result) {
                    resolve(event.target.result.data);
                } else {
                    resolve(null);
                }
            };

            getRequest.onerror = function (event) {
                reject(event.target.error);
            };
        };

        request.onerror = function (event) {
            reject(event.target.error);
        };
    });
}