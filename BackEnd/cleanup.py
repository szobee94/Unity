import os
import glob
import time
import datetime
import multiprocessing
import shutil


def print_wt(message: str):
    print(str(datetime.datetime.now()) + ": " + message)


class CleanupThread(multiprocessing.Process):
    """
        A single-thread function, which sequentially deletes files from the target path.
        The file names must match the "chest_0_*.log" regular expression.
    """
    def __init__(self,
                 path: str = "/dev/shm",
                 interval: float = 2.0,
                 threshold_lower: int = 50,
                 threshold_upper: int = 500,
                 save_path: str = None,
                 *args,
                 **kwargs):
        """
        :param path: The path of the target folder, from which the files will be deleted.
        :param interval: The time interval in seconds, which gives that how frequently
        the function should check the target folder.
        :param threshold_lower: The number of newest files that should remain in the target
        folder after deletion.
        :param threshold_upper: The deletion should occur when at least this many files
        are in the target folder.
        """
        super(CleanupThread, self).__init__(*args, **kwargs)
        self.path = path
        self.interval = interval
        self.threshold_lower = threshold_lower
        self.threshold_upper = threshold_upper
        self.save_path = save_path
        self._stop = multiprocessing.Event()
        self._cleanup_ev = multiprocessing.Event()

    def stop(self):
        self._stop.set()

    def cleanup(self):
        self._cleanup_ev.set()

    def delete_or_move(self, file_name: str):
        source = self.path + '/' + file_name
        if self.save_path is not None:
            destination = self.save_path + '/' + file_name
            shutil.move(source, destination)
        else:
            os.remove(source)

    def _cleanup(self, name: str):
        files = [os.path.basename(fn) for fn in glob.glob(self.path + "/chest_*_*.log")]
        files.sort()
        for fname in files:
            self.delete_or_move(fname)
        if len(files) > 0:
            operation = 'Moved' if self.save_path is not None else 'Deleted'
            message = operation + ' ' + str(len(files)) + ' files (' + files[0] + ' - ' + files[-1] + ')'
            if self.save_path is not None:
                message += ' to ' + self.save_path
            message += ' at the ' + name + ' cleanup.'
            print_wt(message)

    def _cleanup_with_threshold(self, files):
        print_wt("Initiating cleanup...")
        files.sort()
        limit = len(files) - self.threshold_lower
        for i in range(limit):
            self.delete_or_move(files[i])
        first_deleted = files[0]
        last_deleted = files[limit - 1]
        operation = 'Moved' if self.save_path is not None else 'Deleted'
        message = operation + ' ' + str(limit) + ' files (' + first_deleted + ' - ' + last_deleted + ')'
        if self.save_path is not None:
            message += ' to ' + self.save_path
        print_wt(message)

    def run(self):
        print_wt('Cleanup thread starting...')

        self._cleanup("initial")
        while not self._stop.wait(self.interval):
            if self._cleanup_ev.is_set():
                self._cleanup_ev.clear()
                self._cleanup("instructed")

            files = [os.path.basename(fn) for fn in glob.glob(self.path + "/chest_*_*.log")]
            if len(files) >= self.threshold_upper:
                print_wt("Number of files (" + str(len(files)) + ") "
                         "reached the given treshold of " + str(self.threshold_upper))
                self._cleanup_with_threshold(files)
            time.sleep(self.interval)
        self._stop.clear()
        print_wt('Cleanup thread terminating...')
        self._cleanup("terminal")
