from abc import ABCMeta, abstractmethod


class AbstractIl2cppRepository(metaclass=ABCMeta):
    @abstractmethod
    def dump(self):
        pass
