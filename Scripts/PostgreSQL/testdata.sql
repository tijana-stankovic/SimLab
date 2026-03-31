-- Test data
INSERT INTO world (uid, name, space, x, y, z, mode)
VALUES  ('test-world-01', 'Test World 01', 2, 40, 25, 0, 'S'),
        ('test-world-02', 'Test World 02', 2, 60, 40, 0, 'A'),
        ('test-world-03', 'Test World 03', 3, 20, 20, 20, 'S'),
        ('test-world-04', 'Test World 04', 2, 0, 0, 0, 'A'),
        ('test-world-05', 'Test World 05', 3, 50, 30, 10, 'S');

INSERT INTO characteristic (world, name, ord)
VALUES  ((SELECT id FROM world WHERE uid = 'test-world-01'), 'energy', 0),
        ((SELECT id FROM world WHERE uid = 'test-world-01'), 'age', 1),
        ((SELECT id FROM world WHERE uid = 'test-world-02'), 'temperature', 0),
        ((SELECT id FROM world WHERE uid = 'test-world-02'), 'pressure', 1),
        ((SELECT id FROM world WHERE uid = 'test-world-03'), 'mass', 0),
        ((SELECT id FROM world WHERE uid = 'test-world-03'), 'charge', 1),
        ((SELECT id FROM world WHERE uid = 'test-world-04'), 'size', 0),
        ((SELECT id FROM world WHERE uid = 'test-world-04'), 'speed', 1),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'alpha', 0),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'beta', 1),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'gamma', 2);

INSERT INTO phase (world, name, method)
VALUES  ((SELECT id FROM world WHERE uid = 'test-world-01'), 'Initialization', 'SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Initialization'),
        ((SELECT id FROM world WHERE uid = 'test-world-01'), 'Update', 'SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Update'),
        ((SELECT id FROM world WHERE uid = 'test-world-02'), 'Initialization', 'SimLabPlugIn2.dll;SimLabPlugIn2.PlugIn2;Initialization'),
        ((SELECT id FROM world WHERE uid = 'test-world-02'), 'Update', 'SimLabPlugIn2.dll;SimLabPlugIn2.PlugIn2;Update'),
        ((SELECT id FROM world WHERE uid = 'test-world-02'), 'Selection', 'SimLabPlugIn2.dll;SimLabPlugIn2.PlugIn2;Selection'),
        ((SELECT id FROM world WHERE uid = 'test-world-03'), 'Initialization', 'SimLabGOL.dll;SimLabGOL.GameOfLife;Initialization'),
        ((SELECT id FROM world WHERE uid = 'test-world-03'), 'PreCycle', 'SimLabGOL.dll;SimLabGOL.GameOfLife;PreCycle'),
        ((SELECT id FROM world WHERE uid = 'test-world-03'), 'ProcessWorld', 'SimLabGOL.dll;SimLabGOL.GameOfLife;ProcessWorld'),
        ((SELECT id FROM world WHERE uid = 'test-world-04'), 'Initialization', 'SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Initialization'),
        ((SELECT id FROM world WHERE uid = 'test-world-04'), 'Evaluation', 'SimLabPlugIn.dll;SimLabPlugIn.PlugIn;Test'),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'Initialization', 'SimLabGOL.dll;SimLabGOL.GameOfLife;Initialization'),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'PreCycle', 'SimLabGOL.dll;SimLabGOL.GameOfLife;PreCycle'),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'ProcessWorld', 'SimLabGOL.dll;SimLabGOL.GameOfLife;ProcessWorld'),
        ((SELECT id FROM world WHERE uid = 'test-world-05'), 'PostCycle', 'SimLabPlugIn2.dll;SimLabPlugIn2.PlugIn2;Selection');

INSERT INTO phase_parameter (phase, ord, value)
VALUES  ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-01' AND upper(p.name) = upper('Initialization')), 0, 'testsim_init.txt'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-01' AND upper(p.name) = upper('Update')), 0, '0.123'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-02' AND upper(p.name) = upper('Initialization')), 0, 'Example2_init.txt'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-02' AND upper(p.name) = upper('Selection')), 0, '40'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-03' AND upper(p.name) = upper('Initialization')), 0, 'GOL.txt'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-04' AND upper(p.name) = upper('Initialization')), 0, 'testsim_init.txt'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-05' AND upper(p.name) = upper('Initialization')), 0, 'GOL.txt'),
        ((SELECT p.id FROM phase p JOIN world w ON w.id = p.world WHERE w.uid = 'test-world-05' AND upper(p.name) = upper('PostCycle')), 0, 'xxx');
