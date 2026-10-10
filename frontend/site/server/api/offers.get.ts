import { offerNotes, offers } from '../mock/promotions'

export default defineEventHandler(() => ({ items: offers, notes: offerNotes }))
