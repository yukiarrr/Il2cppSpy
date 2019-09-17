from abc import ABCMeta, abstractmethod


class AbstractFileRepository(metaclass=ABCMeta):
    @abstractmethod
    def decode(self):
        pass
