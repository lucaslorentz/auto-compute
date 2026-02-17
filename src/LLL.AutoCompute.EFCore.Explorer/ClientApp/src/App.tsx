import React from "react";
import { Route, Routes, Navigate } from "react-router-dom";
import { EntitySchemaList } from "./components/EntitySchemaList";
import { EntitySchemaDetails } from "./components/EntitySchemaDetails";
import { EntityItemList } from "./components/EntityItemList";
import { EntityItemDetails } from "./components/EntityItemDetails";
import { ComputedMembersList } from "./components/ComputedMembersList";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<EntitySchemaList />} />
      <Route path=":name/schema" element={<EntitySchemaDetails />} />
      <Route path=":name/items" element={<EntityItemList />} />
      <Route path=":name/items/:id" element={<EntityItemDetails />} />
      <Route path="computeds" element={<ComputedMembersList />} />
    </Routes>
  );
}
