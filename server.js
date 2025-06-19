const express = require('express');
const nodemailer = require('nodemailer');
const { v4: uuidv4 } = require('uuid');

const app = express();
app.use(express.json());

// In-memory stores
const users = []; // {id, username, email}
const forms = []; // {id, title, requireLogin, notifyByEmail, ownerId}
const responses = []; // {formId, answers}

// Configure email transport - adjust with real credentials
const transporter = nodemailer.createTransport({
  host: process.env.SMTP_HOST || 'localhost',
  port: process.env.SMTP_PORT || 1025,
  secure: false,
  auth: process.env.SMTP_USER
    ? { user: process.env.SMTP_USER, pass: process.env.SMTP_PASS }
    : null,
});

// Register a user
app.post('/register', (req, res) => {
  const { username, email } = req.body;
  if (!username || !email) return res.status(400).json({ error: 'username and email required' });
  const user = { id: uuidv4(), username, email };
  users.push(user);
  res.status(201).json(user);
});

// Create a form
app.post('/forms', (req, res) => {
  const { title, requireLogin, notifyByEmail, ownerId } = req.body;
  if (!ownerId || !users.find(u => u.id === ownerId)) return res.status(400).json({ error: 'invalid owner' });
  const form = { id: uuidv4(), title, requireLogin: !!requireLogin, notifyByEmail: !!notifyByEmail, ownerId };
  forms.push(form);
  res.status(201).json(form);
});

// Submit a form response
app.post('/forms/:id/responses', (req, res) => {
  const form = forms.find(f => f.id === req.params.id);
  if (!form) return res.status(404).json({ error: 'form not found' });
  responses.push({ formId: form.id, answers: req.body });

  // send email if notifications enabled
  if (form.notifyByEmail) {
    const owner = users.find(u => u.id === form.ownerId);
    if (owner) {
      transporter.sendMail({
        from: 'no-reply@example.com',
        to: owner.email,
        subject: `New response for form ${form.title}`,
        text: `Your form has a new response:\n${JSON.stringify(req.body, null, 2)}`,
      }).catch(err => console.error('Email error:', err));
    }
  }

  res.status(201).json({ message: 'response recorded' });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`Server running on port ${PORT}`));
