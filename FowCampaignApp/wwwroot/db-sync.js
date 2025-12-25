export async function writeDatabaseToOpfs(filename, byteArray) {
    try {
        const root = await navigator.storage.getDirectory();
        const fileHandle = await root.getFileHandle(filename, { create: true });
        
        const writable = await fileHandle.createWritable();
        
        await writable.write(byteArray);
        await writable.close();
        console.log("Database synced from cloud successfully.");
        return true;
    } catch (err) {
        console.error("Error syncing DB:", err);
        return false;
    }
}

export async function readDatabaseFromOpfs(filename) {
    try {
        const root = await navigator.storage.getDirectory();
        const fileHandle = await root.getFileHandle(filename);
        const file = await fileHandle.getFile();
        const arrayBuffer = await file.arrayBuffer();
        return new Uint8Array(arrayBuffer);
    } catch (err) {
        console.error("Error reading DB:", err);
        return null;
    }
}