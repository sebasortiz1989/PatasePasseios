-- Demo dataset for Patas & Passeios.
--
-- Populates a database that already has the schema and the seeded demo account
-- (run the app once first). Wipes all records except the PetSitter account.
--
-- Shaped so every screen has something to show: four tutors, eight dogs, all
-- four service types, work that is done-and-paid, done-and-unpaid, and upcoming,
-- plus one tutor carrying credit so the payment ledger is visible rather than
-- theoretical.
--
-- Dates are written by Scripts/seed-demo.sh relative to today, so the agenda
-- always straddles now. Do not run this file directly.

DELETE FROM TutorPaymentAllocations;
DELETE FROM TutorPayments;
DELETE FROM WalkingService;
DELETE FROM PetHotelService;
DELETE FROM DayCareService;
DELETE FROM PetSittingService;
DELETE FROM Dogs;
DELETE FROM PetSitterTutors;
DELETE FROM Tutors;

-- Placeholder people. Never put real tutors in a file that goes into git.
INSERT INTO Tutors (TutorId, Name, Telephone, Address, Credit) VALUES
 (1,'Ana Silva','(13) 99812-4471','Rua das Flores, 100', 0),
 (2,'Bruno Carvalho','(13) 99745-2280','Av. Ana Costa, 425', 0),
 (3,'Carla Mendes','(13) 99123-8890','Rua Oswaldo Cruz, 78', 35.00),
 (4,'Diego Ramos','(13) 99660-1145','Rua Jorge Tibiriçá, 210', 0);

INSERT INTO PetSitterTutors (PetSitterId, TutorId) VALUES (1,1),(1,2),(1,3),(1,4);

INSERT INTO Dogs (DogId, TutorId, Name, Breed, Description) VALUES
 (1,1,'Thor','Golden Retriever','Puxa a guia no começo do passeio. Adora água.'),
 (2,1,'Maia','Border Collie','Muita energia. Precisa de passeio longo.'),
 (3,2,'Bolinha','SRD','Tímido com outros cães. Melhor sozinho.'),
 (4,2,'Nina','Shih Tzu','Idosa, passeio curto. Remédio às 18h.'),
 (5,3,'Rex','Pastor Alemão','Obediente. Não gosta de moto.'),
 (6,3,'Luna','Poodle','Late no elevador. Chave com o porteiro.'),
 (7,4,'Simba','Bulldog Francês','Cansa rápido no calor. Sem sol forte.'),
 (8,4,'Frida','Dachshund','Escapa por baixo do portão.');

-- Passeios: seven settled, three done but unpaid, five upcoming.
INSERT INTO WalkingService (DogId,PetSitterId,Date,Price,Discount,ServicePaid,ServiceDone,AmountSettled,CreditApplied) VALUES
 (1,1,'{{D-9}}',45,0,1,1,45,0), (2,1,'{{D-9}}',45,0,1,1,45,0),
 (3,1,'{{D-8}}',40,0,1,1,40,0), (5,1,'{{D-7}}',50,0,1,1,50,0),
 (6,1,'{{D-7}}',45,0,1,1,45,0), (1,1,'{{D-5}}',45,0,1,1,45,0),
 (8,1,'{{D-4}}',40,0,1,1,40,0),
 (2,1,'{{D-3}}',45,0,0,1,0,0), (5,1,'{{D-2}}',50,0,0,1,0,0), (3,1,'{{D-1}}',40,0,0,1,0,0),
 (1,1,'{{D+1}}',45,0,0,0,0,0), (6,1,'{{D+1}}',45,0,0,0,0,0),
 (2,1,'{{D+2}}',45,0,0,0,0,0), (7,1,'{{D+3}}',40,0,0,0,0,0), (4,1,'{{D+4}}',35,0,0,0,0,0);

-- Hotel: one closed stay with an extra charge, one booked ahead.
INSERT INTO PetHotelService (DogId,PetSitterId,StartDate,EndDate,PricePerDay,ExtraCharge,Discount,RequiresWalking,ServicePaid,ServiceDone,AmountSettled,CreditApplied) VALUES
 (5,1,'{{D-6}}','{{D-3}}',90,40,0,1,1,1,310,0),
 (7,1,'{{D+5}}','{{D+9}}',85,0,0,1,0,0,0,0);

INSERT INTO DayCareService (DogId,PetSitterId,Date,Price,RequiresWalking,Discount,ServicePaid,ServiceDone,AmountSettled,CreditApplied) VALUES
 (4,1,'{{D-5}}',70,0,0,1,1,70,0),
 (8,1,'{{D-2}}',70,1,0,0,1,0,0),
 (3,1,'{{D+2}}',70,0,0,0,0,0,0);

INSERT INTO PetSittingService (DogId,PetSitterId,Date,Price,Discount,ServicePaid,ServiceDone,AmountSettled,CreditApplied) VALUES
 (6,1,'{{D-4}}',120,0,1,1,120,0),
 (1,1,'{{D+6}}',120,0,0,0,0,0);

-- Carla overpaid: 560 against 525 of work, 35 banked as credit.
INSERT INTO TutorPayments (TutorPaymentId,TutorId,PetSitterId,Date,Amount,CreditStored) VALUES
 (1,1,1,'{{D-6}}',135,0),
 (2,2,1,'{{D-5}}',110,0),
 (3,3,1,'{{D-3}}',560,35),
 (4,4,1,'{{D-2}}',40,0);

UPDATE PetSitter SET Pix = '(13) 99812-0000' WHERE PetSitterId = 1;
