mergeInto(LibraryManager.library, {
  SyncFiles: function () {
    FS.syncfs(false, function (err) {
      if (err) {
        console.error("Sync error", err);
      } else {
        console.log("Sync successful");
      }
    });
  }
});